using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.ImageUtilities;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ValorVDC_BIMTools.Commands.SpecifyLength;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class SpecifyLength : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var handler = new SpecifyLengthHandler(commandData);
        var externalEvent = ExternalEvent.Create(handler);

        var specifyLengthWindow = new SpecifyLengthWindow();
        var showDialog = specifyLengthWindow.ShowDialog();
        if (showDialog != true)
            return Result.Cancelled;

        if (specifyLengthWindow.SpecifiedLength == null)
        {
            TaskDialog.Show("Error", "You must provide a valid length.");
            return Result.Cancelled;
        }

        handler.SelectedLength = specifyLengthWindow.SpecifiedLength.Value;

        handler.KeepRunning = true;
        externalEvent.Raise();

        return Result.Succeeded;
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
            LargeImage = ImagineUtilities.LoadImage(assembly, "falcon.png")
        });
}
}