using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands.CopyScopeBoxes.Views;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Commands;

[Transaction(TransactionMode.Manual)]
public class CopyScopeBoxesCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var document = commandData.Application.ActiveUIDocument.Document;
            var window = new CopyScopeBoxesView(document);
            window.ShowDialog();
            return Result.Succeeded;

        }
        catch (Exception e)
        {
            message = e.Message;
            return Result.Failed;
        }
    }
    
    public static void CreateButton(RibbonPanel panel)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var buttonName = "Copy Scope Boxes";
        var buttonText = "Copy" + Environment.NewLine + "Scope Boxes";
        var className = MethodBase.GetCurrentMethod().DeclaringType.FullName;
        panel.AddItem(
            new PushButtonData(buttonName, buttonText, assembly.Location, className)
            {
                ToolTip = "Copies Selected Scope Boxes",
                LargeImage = ImagineUtilities.LoadImage(assembly, "stormTrooper.png")
            });
    }
}