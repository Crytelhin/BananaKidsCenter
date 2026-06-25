using System.Globalization;

namespace EntertainmentCenter.Converters;

public class BoolToBorderColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        
        if (isActive)
        {
            if (Application.Current != null && Application.Current.Resources.TryGetValue("BluePrimary", out var blueColor))
            {
                return (Color)blueColor;
            }
            return Color.FromArgb("#2563EB"); // Fallback to BluePrimary hex
        }
        else
        {
            if (Application.Current != null && Application.Current.Resources.TryGetValue("Border", out var borderColor))
            {
                return (Color)borderColor;
            }
            return Color.FromArgb("#E8E5DD"); // Fallback to Border color hex
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
