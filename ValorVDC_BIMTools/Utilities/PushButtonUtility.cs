using Autodesk.Revit.UI;
using ValorVDC_BIMTools.ImageUtilities;

namespace ValorVDC_BIMTools.Utilities;

public class PushButtonUtility
{
    public static PushButton CreatePushButton(
        RibbonPanel ribbonPanel,
        string buttonName,
        string buttontext,
        string toolTip,
        string imageName,
        Type declaringType)
    {
        var assembly = declaringType.Assembly;
        var className = declaringType.FullName;
        var pushButton = ribbonPanel.AddItem(
            new PushButtonData(buttonName, buttontext, assembly.Location, className)
            {
                ToolTip = toolTip,
                LargeImage = ImagineUtilities.LoadImage(assembly, imageName)
            }) as PushButton;
        return pushButton;
    }
}