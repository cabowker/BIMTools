using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Microsoft.Win32;

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

    public static BitmapImage LoadThemeImage(Assembly assembly, string lightImageName, string darkImageName)
    {
        try
        {
            var isDarkMode = isRevitInDarkMode();
            var imageName = isDarkMode ? darkImageName : lightImageName;
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(imageName));

            if (resourceName == null && isDarkMode)
                resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(lightImageName));
            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.EndInit();
                    return image;
                }
            }
        }
        catch (Exception e)
        {
            return LoadImage(assembly, lightImageName);
        }

        return new BitmapImage();
    }

    private static bool isRevitInDarkMode()
    {
        try
        {
            return UIThemeManager.CurrentTheme == UITheme.Dark;
        }
        catch (Exception)
        {
            try
            {
                var currentTheme = UIThemeManager.CurrentTheme;
                return currentTheme.ToString().Contains("Dark", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // Method 3: Check system theme as fallback
                return IsSystemInDarkMode();
            }
        }
    }

    private static bool IsSystemInDarkMode()
    {
        try
        {
            // Check Windows registry for system theme
            using (var key = Registry.CurrentUser.OpenSubKey(
                       @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key?.GetValue("AppsUseLightTheme") is int useLightTheme)
                    return useLightTheme == 0; // 0 = dark mode, 1 = light mode
            }
        }
        catch (Exception)
        {
            // If registry check fails, assume light mode
        }

        return false;
    }

    public static void UpdateButtonIcon(PushButton button, Assembly assembly, string lightImageName,
        string darkImageName)
    {
        try
        {
            var newImage = LoadThemeImage(assembly, lightImageName, darkImageName);
            button.Image = newImage;
            button.LargeImage = newImage;
        }
        catch (Exception)
        {
            // If update fails, keep the existing image
        }
    }
}