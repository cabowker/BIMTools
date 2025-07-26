using System;
using System.Linq;
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
    private static readonly List<(PushButton button, Assembly assembly, string lightImage, string darkImage)>
        _themeAwareButtons = new();

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

        application.ThemeChanged += (s, e) =>
        {
            foreach (var (button, assembly, lightImage, darkImage) in _themeAwareButtons)
                ImagineUtilities.UpdateButtonIcon(button, assembly, lightImage, darkImage);
        };


        return Result.Succeeded;
    }


    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    public static void CreateThemeAwareButton(RibbonPanel panel, Assembly assembly, string buttonName,
        string buttonText, string className, string lightImageName, string darkImageName, string toolTip)
    {
        var buttonData = new PushButtonData(buttonName, buttonText, assembly.Location, className)
        {
            ToolTip = toolTip,
            LargeImage = ImagineUtilities.LoadThemeImage(assembly, lightImageName, darkImageName)
        };

        var button = panel.AddItem(buttonData) as PushButton;

        // Register this button for theme updates
        _themeAwareButtons.Add((button, assembly, lightImageName, darkImageName));
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

        var floorSleeveButtonData = FloorSleeveRound.CreatePushButtonData();
        pulldownButton.AddPushButton(floorSleeveButtonData);

        var roundSleeveButtonData = WallSleevesRound.CreatePushButtonData();
        pulldownButton.AddPushButton(roundSleeveButtonData);

        var rectangularSleeveButtonData = WallSleevesRectangular.CreatePushButtonData();
        pulldownButton.AddPushButton(rectangularSleeveButtonData);

        var realignElement = RealignElement.CreatePushButtonData();
        pulldownButton.AddPushButton(realignElement);

        var realignElements = RealignMultiElements.CreatePushButtonData();
        pulldownButton.AddPushButton(realignElements);
    }
}