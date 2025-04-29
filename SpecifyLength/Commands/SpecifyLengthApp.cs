using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using SpecifyLength.Infrastructure;
using SpecifyLength.ViewModels;
using SpecifyLength.Views;
using ValorVDC_BIMTools.ImageUtilities;

namespace SpecifyLength.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class SpecifyLengthApp : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var handler = new SpecifyLengthHandler(commandData.Application);
        var externalEvent = ExternalEvent.Create(handler);

        var viewModel = new SpecifyLengthViewModel(commandData.Application);
        var specifyLengthWindow = new SpecifyLengthView(viewModel);
        
        var result = specifyLengthWindow.ShowDialog();
        if (result != true)
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
