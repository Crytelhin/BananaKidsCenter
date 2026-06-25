using System.Globalization;

namespace EntertainmentCenter.Converters;

public class BoolToColorConverter : IValueConverter
{
    // When ConverterParameter is set (e.g. '#FFFFFF'), that color is used for the "true" state
    // and TextPrimary (#1C1917) is used for the "false" state — this is for TextColor bindings.
    // Without a parameter, true → BluePrimary (#2563EB), false → Transparent — for BackgroundColor.
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isActive = value is bool b && b;

        if (!isActive)
        {
            // False state: transparent for background, TextPrimary for text
            if (parameter is string)
                return Color.FromArgb("#1C1917"); // TextPrimary
            return Colors.Transparent;
        }

        // True state
        if (parameter is string colorHex)
            return Color.FromArgb(colorHex);
        return Color.FromArgb("#2563EB"); // BluePrimary
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
