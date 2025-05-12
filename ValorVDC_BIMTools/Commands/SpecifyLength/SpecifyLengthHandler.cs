using System.Windows.Input;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

public class SpecifyLengthHandler : IExternalEventHandler
{
    private readonly ExternalCommandData _commandData;

    public SpecifyLengthHandler(ExternalCommandData commandData)
    {
        _commandData = commandData;
    }

    public double SelectedLength { get; set; }
    public bool KeepRunning { get; set; }

    public void Execute(UIApplication application)
    {
        try
        {
            StopOnEscape();
            if (!KeepRunning)
            {
                Stop();
                return;
            }


            var uiDocument = _commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;


            var pickedReference = uiDocument.Selection.PickObject(ObjectType.Element, "Select an MEP Curve");

            if (pickedReference == null)
            {
                TaskDialog.Show("Cancelled", "No element was selected");
                return;
            }

            var element = document.GetElement(pickedReference.ElementId);
            var mepCurve = element as MEPCurve;

            if (mepCurve == null)
            {
                TaskDialog.Show("Error", "Selected Element is not MEP Curve.");
                return;
            }

            var locationCurve = mepCurve.Location as LocationCurve;

            if (locationCurve == null || !(locationCurve.Curve is Line currentLine))
            {
                TaskDialog.Show("Error", "Selected MEP curve is not a supported linear geometry.");
                return;
            }

            var currentLength = currentLine.Length;
            var adjustmentLength = SelectedLength - currentLength;
            if (Math.Abs(adjustmentLength) < 0.001)
            {
                TaskDialog.Show("Info", "The elements current length already matched the specified length.");
                return;
            }

            var connectors = mepCurve.ConnectorManager.Connectors;
            Connector connector0 = null;
            Connector connector1 = null;

            foreach (Connector connector in connectors)
                if (connector.Id == 0)
                    connector0 = connector;
                else if (connector.Id == 1)
                    connector1 = connector;

            if (connector0 == null || connector1 == null)
            {
                TaskDialog.Show("Error", "MEP Curve does not have the necessary connectors.");
                return;
            }

            var pickedPoint = pickedReference.GlobalPoint;
            var distanceToConnector0 = pickedPoint.DistanceTo(connector0.Origin);
            var distanceToConnector1 = pickedPoint.DistanceTo(connector1.Origin);

            var connectorToExtend = distanceToConnector0 < distanceToConnector1 ? connector1 : connector0;
            var oppositeConnector = connectorToExtend == connector0 ? connector1 : connector0;

            var (adjustedLine, adjustmentDelta) = AdjustCurve(locationCurve, connectorToExtend, oppositeConnector,
                adjustmentLength);

            if (adjustedLine == null)
            {
                TaskDialog.Show("Error", "Failed to adjust MEPCurve.");
                return;
            }

            using (var transaction = new Transaction(document, "adjust MEP Curve"))
            {
                transaction.Start();
                locationCurve.Curve = adjustedLine;

                foreach (Connector connector in connectorToExtend.AllRefs)
                    if (connector.Owner.Id != mepCurve.Id)
                    {
                        var connectedElement = document.GetElement(connector.Owner.Id);
                        if (connectedElement != null)
                        {
                            var location = connectedElement.Location as LocationPoint;
                            if (location != null)
                            {
                                var currentPoint = location.Point;
                                location.Point = currentPoint + adjustmentDelta;
                            }
                        }
                    }

                transaction.Commit();
            }
        }
        catch (OperationCanceledException)
        {
            KeepRunning = false;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"An error occurred:\n{ex.Message}");
        }

        if (KeepRunning)
            ExternalEvent.Create(this).Raise();
    }

    public string GetName()
    {
        return "Specify Length Interaction Handler";
    }


    public void Stop()
    {
        KeepRunning = false;
        TaskDialog.Show("Stopped", "Command has been stopped.");
    }

    private (Line adjustedLine, XYZ adjustmentDelta) AdjustCurve(LocationCurve? locationCurve,
        Connector connectorToAdjust, Connector oppsiteConnector,
        double adjustmentLength)
    {
        if (locationCurve == null || connectorToAdjust == null || oppsiteConnector == null)
            return (null, null);

        var currentLine = locationCurve.Curve as Line;
        if (currentLine == null)
            return (null, null);

        var direction = (currentLine.GetEndPoint(1) - currentLine.GetEndPoint(0)).Normalize();
        var newStartPoint = currentLine.GetEndPoint(0);
        var newEndPoint = currentLine.GetEndPoint(1);
        var adjustmentDelta = XYZ.Zero;


        if (adjustmentLength > 0)
        {
            if (connectorToAdjust.Id == 0)
            {
                adjustmentDelta = -direction * adjustmentLength;
                newStartPoint = newStartPoint + adjustmentDelta;
            }
            else
            {
                adjustmentDelta = direction * adjustmentLength;
                ;
                newEndPoint = newEndPoint + adjustmentDelta;
            }
        }
        else
        {
            if (connectorToAdjust.Id == 0)
            {
                adjustmentDelta = direction * Math.Abs(adjustmentLength);
                newStartPoint = newStartPoint + adjustmentDelta;
            }
            else
            {
                adjustmentDelta = -direction * Math.Abs(adjustmentLength);
                newEndPoint = newEndPoint - direction * Math.Abs(adjustmentLength);
            }
        }

        return (Line.CreateBound(newStartPoint, newEndPoint), adjustmentDelta);
    }

    private void StopOnEscape()
    {
        if (Keyboard.IsKeyDown(Key.Escape))
            KeepRunning = false;
    }
}