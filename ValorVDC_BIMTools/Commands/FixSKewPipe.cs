using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ValorVDC_BIMTools.HelperMethods;
using ValorVDC_BIMTools.ImageUtilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ValorVDC_BIMTools;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class FixSKewPipe : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            while (true)
                try
                {
                    Run(commandData.Application);
                }
                catch (OperationCanceledException e)
                {
                    //user presses to escape
                    break;
                }
        }
        catch (Exception e)
        {
            TaskDialog.Show("Error", $"Could not execute the command {e.Message}");
            return Result.Failed;
        }

        return Result.Succeeded;
    }

    private Result Run(UIApplication uiApplication)
    {
        var uiDocument = uiApplication.ActiveUIDocument;
        var application = uiApplication.Application;
        var document = uiDocument.Document;

        var pickedEnd = uiDocument.Selection.PickObject(ObjectType.Element, new SelectionFilters.MepCurveAndFabFilterWithOutInsulation(),
            "Please select the end you would to keep");
        if (pickedEnd == null)
            return Result.Cancelled;

        var selectedElement = document.GetElement(pickedEnd);

        if (!(selectedElement is MEPCurve element))
        {
            TaskDialog.Show("Error", "Selected element is not a MEP curve.");
            return Result.Failed;
        }

        // Get the connectors of the pipe
        var connectorManager = element.ConnectorManager;

        // Ensure Curve has connectors
        if (connectorManager == null)
        {
            TaskDialog.Show("Error", "The selected element does not contain connectors.");
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
            TaskDialog.Show("Error", "Could not find the start or end points of the selected pipe.");
            return Result.Failed;
        }

        // Identify connected and disconnected ends
        var connectedConnector = startConnector.IsConnected ? startConnector : endConnector;
        var oppositeConnector = startConnector.IsConnected ? endConnector : startConnector;

        // Get the locations of the connected and disconnected ends
        var selectededPoint = connectedConnector.Origin;
        var unSelectedPoint = oppositeConnector.Origin;

        var newLocationPoint = new XYZ(
            selectededPoint.X, // Match X with connected end
            selectededPoint.Y, // Match Y with connected end
            unSelectedPoint.Z // Keep existing Z coordinate
        );

        using (var transaction = new Transaction(document, "Fix Skewed Element"))
        {
            transaction.Start();
            // Get the location of the pipe/duct and verify it's a curve
            var location = element.Location;
            if (location is LocationCurve locationCurve)
            {
                // Retrieve current start and end points
                var startPoint = locationCurve.Curve.GetEndPoint(0);
                var endPoint = locationCurve.Curve.GetEndPoint(1);

                // Determine whether to adjust start or end point based on the disconnected connector
                if (startPoint.IsAlmostEqualTo(unSelectedPoint))
                    // Modify the start point
                    locationCurve.Curve = Line.CreateBound(newLocationPoint, endPoint);
                else if (endPoint.IsAlmostEqualTo(unSelectedPoint))
                    // Modify the end point
                    locationCurve.Curve = Line.CreateBound(startPoint, newLocationPoint);
            }
            else
            {
                TaskDialog.Show("Error", "Selected element does not have a modifiable curve.");
                return Result.Failed;
            }

            transaction.Commit();
        }

        return Result.Succeeded;
    }

    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Fix Skew";
        var buttonText = "Fix Skew" + Environment.NewLine + "Element";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Fix Skewed MEP Element",
                LargeImage = ImagineUtilities.LoadImage(assembly, "mando-32.png")
            });
    }
}