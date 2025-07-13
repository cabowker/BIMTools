using System.Reflection;
using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands;
using ValorVDC_BIMTools.Commands.FlowArrows;
using ValorVDC_BIMTools.Commands.SpecifyLength;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools;

// All tools built for Revit 2024
public class AppCommand : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            application.CreateRibbonTab("BIM Tools");
        }
        catch (Exception)
        {
            TaskDialog.Show("Error", "Could not load Ribbon tab");
        }

        var mepToolsPanel = application.GetRibbonPanels("BIM Tools").FirstOrDefault(x => x.Name == "BIM Tools") ??
                            application.CreateRibbonPanel("BIM Tools", "MEP Tools");

        var modelToolsPanel = application.CreateRibbonPanel("BIM Tools", "Model Tools");


        FixSKewPipe.CreateButton(mepToolsPanel);
        DisconnectPipe.CreateButton(mepToolsPanel);
        SpecifyLength.CreateButton(mepToolsPanel);
        FlowArrow.CreateButton(mepToolsPanel);

        CreateSleevesPulldownButton(mepToolsPanel);

        CopyScopeBoxesCommand.CreateButton(modelToolsPanel);
        ZoomObject.CreateButton(modelToolsPanel);


        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private void CreateSleevesPulldownButton(RibbonPanel ribbonPanel)
    {
        var assembly = Assembly.GetExecutingAssembly();


        var pulldownButtonData = new PulldownButtonData("SleevesButton", "Sleeve Tools")
        {
            ToolTip = "Wall Sleeves Tools",
            LargeImage = ImagineUtilities.LoadImage(assembly, "3peo.png")
        };

        var pulldownButton = ribbonPanel.AddItem(pulldownButtonData) as PulldownButton;


        var roundButtonData = WallSleevesRound.CreatePushButtonData();
        pulldownButton.AddPushButton(roundButtonData);

        var rectangularButtonData = WallSleevesRectangular.CreatePushButtonData();
        pulldownButton.AddPushButton(rectangularButtonData);

        var realignElement = RealignElement.CreatePushButtonData();
        pulldownButton.AddPushButton(realignElement);

        var realignElements = RealignMultiElements.CreatePushButtonData();
        pulldownButton.AddPushButton(realignElements);
    }
}