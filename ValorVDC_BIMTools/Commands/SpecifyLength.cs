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
        // Create the ViewModel and inject the necessary Revit dependencies
        var viewModel = new SpecifyLengthViewModel(commandData.Application);

        // Create and show the SpecifyLengthView as a modeless window
        var window = new SpecifyLengthView(viewModel);
        window.Show(); // Modeless display; does not block execution

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