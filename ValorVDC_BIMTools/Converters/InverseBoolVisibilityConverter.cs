using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Visibility = System.Windows.Visibility;

namespace ValorVDC_BIMTools.Converters;

public class InverseBoolVisibilityConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value! == false ? Visibility.Visible : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Visibility)value! != Visibility.Visible;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}