using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands.SpecifyLength;
using ValorVDC_BIMTools.ImageUtilities;
using ValorVDC_BIMTools.SpecifyLength.ViewModels;
using ValorVDC_BIMTools.SpecifyLength.Views;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace SpecifyLength;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class SpecifyLength : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // Here we correctly pass an ExternalCommandData instance to the SpecifyLengthHandler constructor
        var handler = new SpecifyLengthHandler(commandData);

        // Pass the handler to the ViewModel
        var viewModel = new SpecifyLengthViewModel(handler);

        // Create and bind the window
        var window = new SpecifyLengthWindow(viewModel);
        window.Show(); // Display modelessly

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