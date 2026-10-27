using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using WindowsUdk.UI.Shell.PowerUX;
using WinRT;

namespace MobileShell.Converters;

public partial class BatteryIconFillColorConverter : IValueConverter
{
    [DynamicWindowsRuntimeCast(typeof(BatteryIconFillColor))]
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (BatteryIconFillColor)value switch
        {
            BatteryIconFillColor.Default => new SolidColorBrush(Color.FromArgb(255, 142, 142, 142)),
            BatteryIconFillColor.Adequate => new SolidColorBrush(Color.FromArgb(255, 52, 199, 89)),
            BatteryIconFillColor.Warning => new SolidColorBrush(Color.FromArgb(255, 255, 159, 10)),
            BatteryIconFillColor.Critical => new SolidColorBrush(Color.FromArgb(255, 255, 59, 48)),
            _ => new SolidColorBrush(Color.FromArgb(255, 142, 142, 142)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
