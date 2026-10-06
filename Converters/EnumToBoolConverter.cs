using System;
using System.Globalization;
using System.Windows.Data;

namespace POS_BIGA.Converters
{
    /// <summary>
    /// ????? ???? Enum ??? bool ??????? ?? ConverterParameter.
    /// ????? ???? RadioButton.IsChecked ?????? enum.
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(bool))]
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null)
                return Enum.Parse(targetType, parameter.ToString()!);
            return Binding.DoNothing;
        }
    }
}
