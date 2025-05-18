using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands;
using ValorVDC_BIMTools.Commands.FlowArrows;
using ValorVDC_BIMTools.Commands.SpecifyLength;

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

        var ribbonPanel = application.GetRibbonPanels("BIM Tools").FirstOrDefault(x => x.Name == "BIM Tools") ??
                          application.CreateRibbonPanel("BIM Tools", "MEP Tools");

        FixSKewPipe.CreateButton(ribbonPanel);
        DisconnectPipe.CreateButton(ribbonPanel);
        SpecifyLength.CreateButton(ribbonPanel);
        FlowArrow.CreateButton(ribbonPanel);
        WallSleevesRound.CreateButton(ribbonPanel);

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}