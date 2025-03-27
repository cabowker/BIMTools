using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ValorVDC_BIMTools.ImageUtilities;

public class ImagineUtilities 
{
    public static BitmapImage LoadImage(Assembly assembly, string name)
    {
        BitmapImage image = new BitmapImage();
        try
        {
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x=> x.Contains(name));

            if (resourceName != null)
            {
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
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