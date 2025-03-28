using Autodesk.Revit.UI;
using ValorVDC_BIMTools.Commands;

namespace ValorVDC_BIMTools;

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

        var ribbonPanel = application.GetRibbonPanels("BIM Tools").FirstOrDefault(
                              x => x.Name == "BIM Tools") ??
                          application.CreateRibbonPanel("BIM Tools", "MEP Tools");

        FixSKewPipe.CreateButton(ribbonPanel);
        DisconnectPipe.CreateButton(ribbonPanel);
        
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}