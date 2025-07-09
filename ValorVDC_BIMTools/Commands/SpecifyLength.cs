using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class SpecifyLength : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var specifyLengthWindow = new SpecifyLengthWindow();
            var result = specifyLengthWindow.ShowDialog();

            if (result != true || !specifyLengthWindow.SpecifiedLength.HasValue)
                return Result.Cancelled;

            var specifyLengthHandler = new SpecifyLengthHandler(commandData)
            {
                SelectedLength = specifyLengthWindow.SpecifiedLength.Value,
                keepRunning = true
            };

            var externalEvent = ExternalEvent.Create(specifyLengthHandler);
            externalEvent.Raise();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
            return Result.Failed;
        }
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
                LargeImage = ImagineUtilities.LoadImage(assembly, "stormTrooper.png")
            });
    }
}