using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.ImageUtilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class SpecifyLength : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        var document = uiDocument.Document;

        try
        {
            var pickedReference = uiDocument.Selection.PickObject(ObjectType.Element, "Select an MEP Curve");

            if (pickedReference == null)
                return Result.Cancelled;

            var element = document.GetElement(pickedReference.ElementId);
            var mepCurve = element as MEPCurve;

            if (mepCurve == null)
            {
                TaskDialog.Show("Error", "Selected Element is not MEP Curve.");
                return Result.Failed;
            }

            var locationCurve = mepCurve.Location as LocationCurve;

            if (locationCurve == null || !(locationCurve.Curve is Line currentLine))
            {
                TaskDialog.Show("Error", "Selected MEP curve is not a supported linear geometry.");
                return Result.Failed;
            }

            double currentLength = currentLine.Length;
            // Ask the user for their desired length
            var inputWindow = new SpecifyLengthWindow(currentLength);
            bool? dialogResult = inputWindow.ShowDialog();

            if (dialogResult != true || inputWindow.SpecifiedLength == null)
                return Result.Cancelled;

            double specifiedLength = inputWindow.SpecifiedLength.Value;
            double adjustmentLength = specifiedLength - currentLength;
            if (Math.Abs(adjustmentLength) < 0.001)
            {
                TaskDialog.Show("Info", "The elements current length already matched the specified length.");
                return Result.Succeeded;
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
                return Result.Failed;
            }

            var pickedPoint = pickedReference.GlobalPoint;
            var distanceToConnector0 = pickedPoint.DistanceTo(connector0.Origin);
            var distanceToConnector1 = pickedPoint.DistanceTo(connector1.Origin);

            var connectorToExtend = distanceToConnector0 < distanceToConnector1 ? connector1 : connector0;
            var oppositeConnector = connectorToExtend == connector0 ? connector1 : connector0;

            var (adjustedLine, adjustmentDelta) = AdjustCurve(locationCurve, connectorToExtend, oppositeConnector, adjustmentLength);

            if (adjustedLine == null)
            {
                TaskDialog.Show("Error", "Failed to adjust MEPCurve.");
                return Result.Failed;
            }

            using (var transaction = new Transaction(document, "adjust MEP Curve"))
            {
                transaction.Start();
                locationCurve.Curve = adjustedLine;

                foreach (Connector connector in connectorToExtend.AllRefs)
                {
                    if (connector.Owner.Id != mepCurve.Id)
                    {
                        var connectedElement = document.GetElement(connector.Owner.Id);
                        if (connectedElement != null)
                        {
                            var location = connectedElement.Location as LocationPoint;
                            if (location != null)
                            {
                                XYZ currentPoint = location.Point;
                                location.Point = currentPoint + adjustmentDelta;
                            }
                        }
                    }
                    
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }

        catch (OperationCanceledException)
        {
            return Result.Cancelled;
        }

        catch (Exception)
        {
            TaskDialog.Show("Error", "There was an issue, command abouted");
            return Result.Failed;
        }
    }

    private (Line adjustedLine, XYZ adjustmentDelta) AdjustCurve(LocationCurve? locationCurve, Connector connectorToAdjust, Connector oppsiteConnector,
        double adjustmentLength)
    {
        if (locationCurve == null || connectorToAdjust == null || oppsiteConnector == null)
            return (null, null);

        var currentLine = locationCurve.Curve as Line;
        if (currentLine == null)
            return (null, null);

        XYZ direction = (currentLine.GetEndPoint(1) - currentLine.GetEndPoint(0)).Normalize();
        XYZ newStartPoint = currentLine.GetEndPoint(0);
        XYZ newEndPoint = currentLine.GetEndPoint(1);
        XYZ adjustmentDelta = XYZ.Zero;


        if (adjustmentLength > 0)
        {
            if (connectorToAdjust.Id == 0)
            {
                adjustmentDelta = -direction * adjustmentLength;
                newStartPoint = newStartPoint + adjustmentDelta;
            }
            else
            {
                adjustmentDelta = direction * adjustmentLength;;
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
    
    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Specify Length";
        var buttonText = "Specify" + Environment.NewLine + "Length";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Specify Length of Pipe, Duct, or Conduit",
                LargeImage = ImagineUtilities.LoadImage(assembly, "lightSaber.png")
            });
    }
}