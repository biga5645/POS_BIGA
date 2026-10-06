using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POS_BIGA.Converters
{
    public class NegativeToBrushConverter : IValueConverter
    {
        public Brush NegativeBrush { get; set; } = Brushes.DarkRed;
        public Brush PositiveBrush { get; set; } = Brushes.Black;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return PositiveBrush;

            if (decimal.TryParse(value.ToString(), out var d))
            {
                return d < 0m ? NegativeBrush : PositiveBrush;
            }

            return PositiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
