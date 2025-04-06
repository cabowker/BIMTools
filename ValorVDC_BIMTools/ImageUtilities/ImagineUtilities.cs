using System.Reflection;
using System.Windows.Media.Imaging;

namespace ValorVDC_BIMTools.ImageUtilities;

public class ImagineUtilities
{
    public static BitmapImage LoadImage(Assembly assembly, string name)
    {
        var image = new BitmapImage();
        try
        {
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(name));

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.EndInit();
                }
            }
        }
        catch (Exception)
        {
            //ignore
        }

        return image;
    }
}