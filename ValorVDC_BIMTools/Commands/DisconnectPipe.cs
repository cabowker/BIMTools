using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class DisconnectPipe : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        var document = uiDocument.Document;

        var pickedEnd =
            uiDocument.Selection.PickObject(ObjectType.Element, "Select end of Any Curved Element to disconnect");
        if (pickedEnd == null)
            return Result.Cancelled;

        // Get the selected pipe
        var selectedElement = document.GetElement(pickedEnd);

        if (!(selectedElement is MEPCurve element))
        {
            message = "Selected element is not a MEP curve.";
            return Result.Failed;
        }

        var selectedEndPoint = pickedEnd.GlobalPoint;

        using (var transaction = new Transaction(document, "Disconnect Selected Pipe End"))
        {
            transaction.Start();

            // Get the connectors of the pipe
            var connectorManager = element.ConnectorManager;

            // Ensure Curve has connectors
            if (connectorManager == null)
            {
                message = "The selected element does not contain connectors.";
                return Result.Failed;
            }

            // Get the connectors
            Connector startConnector = null;
            Connector endConnector = null;

            // Iterate over the connectors
            foreach (Connector connector in connectorManager.Connectors)
                if (connector.ConnectorType == ConnectorType.End)
                {
                    // Asign the start and end connectors
                    if (startConnector == null)
                        startConnector = connector;
                    else
                        endConnector = connector;
                }

            // Ensure both connectors were found
            if (startConnector == null || endConnector == null)
            {
                message = "Could not find the start or end points of the selected pipe.";
                return Result.Failed;
            }

            // Find out which point is closet to the selected end of pipe
            var curveStart = startConnector.Origin;
            var curveEnd = endConnector.Origin;

            var distanceToStart = curveStart.DistanceTo(selectedEndPoint);
            var distanceToEnd = curveEnd.DistanceTo(selectedEndPoint);

            var selectedConnector = distanceToStart < distanceToEnd ? startConnector : endConnector;

            // Diconnect the selected end
            if (selectedConnector.IsConnected)
                foreach (Connector connectedConnector in selectedConnector.AllRefs)
                    if (connectedConnector.Owner.Id != element.Id)
                    {
                        selectedConnector.DisconnectFrom(connectedConnector);
                        break;
                    }

            transaction.Commit();
        }

        return Result.Succeeded;
    }
    
    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Disconnect Pipe";
        var buttonText = "Disconnect" + Environment.NewLine + "Pipe";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Disconnect Pipe at Selected End",
                LargeImage = ImagineUtilities.LoadImage(assembly, "vader-32.png")
            });
    }
}