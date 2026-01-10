using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace PixelForge.Editor.Converters;

public sealed class BooleanInverterConverter : IValueConverter
{
    public static readonly BooleanInverterConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : AvaloniaProperty.UnsetValue;
    }
}
