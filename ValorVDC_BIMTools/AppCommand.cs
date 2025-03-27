using Autodesk.Revit.UI;

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
            //ignore
        }

        var ribbonPanel = application.GetRibbonPanels("BIM Tools").FirstOrDefault(
                              x => x.Name == "BIM Tools") ??
                          application.CreateRibbonPanel("BIM Tools", "MEP Tools");

        FixSKewPipe.CreateButton(ribbonPanel);
        
        
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
}