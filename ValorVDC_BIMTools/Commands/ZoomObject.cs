using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Utilities;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
public class ZoomObject : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var selectedIds = uiDoc.Selection.GetElementIds().ToList();

        if (!selectedIds.Any())
        {
            TaskDialog.Show("Zoom to Object", "Please select one or more objects first.");
            return Result.Cancelled;
        }

        try
        {
            // Use the new reusable utility method
            RevitUIUtils.ZoomToElements(uiDoc, selectedIds);
            
            // Restore the selection
            uiDoc.Selection.SetElementIds(selectedIds);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }


    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Zoom Object";
        var buttonText = "Zoom" + Environment.NewLine + "Object";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        AppCommand.CreateThemeAwareButton(
            panel,
            assembly,
            buttonName,
            buttonText,
            className,
            "ZoomToButton_32x32.png",
            "DarkZoomToDarkButton_32x32.png",
            "Zooms to Selected Object from another view."
        );
    }
}