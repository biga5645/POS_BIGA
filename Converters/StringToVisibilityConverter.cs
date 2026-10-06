using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace POS_BIGA.Converters
{
    // Converts a string (e.g., ImageUrl) to Visibility.Visible if not null/empty, otherwise Collapsed.
    // If ConverterParameter equals "Invert" (case-insensitive) the result is inverted.
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool has = value is string s && !string.IsNullOrWhiteSpace(s);
            bool invert = false;
            if (parameter is string p && string.Equals(p, "Invert", StringComparison.OrdinalIgnoreCase)) invert = true;
            if (invert) has = !has;
            return has ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
