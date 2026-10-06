using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using POS_BIGA.Models;

namespace POS_BIGA.ViewModels
{
    /// <summary>
    /// محول يحول حالة الدين إلى لون
    /// </summary>
    public class CreditStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CreditStatus status)
            {
                return status switch
                {
                    CreditStatus.Unpaid => new SolidColorBrush(Color.FromRgb(244, 67, 54)),   // أحمر - غير مدفوع
                    CreditStatus.Paid => new SolidColorBrush(Color.FromRgb(76, 175, 80)),     // أخضر - مدفوع
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))                    // رمادي - غير معروف
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// محول يحول حالة الدين إلى نص
    /// </summary>
    public class CreditStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CreditStatus status)
            {
                return status switch
                {
                    CreditStatus.Unpaid => "غير مدفوع",
                    CreditStatus.Paid => "مدفوع",
                    _ => "غير معروف"
                };
            }
            return "غير معروف";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// محول يتحقق إذا كانت القيمة أكبر من قيمة أخرى
    /// </summary>
    public class IsGreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            try
            {
                decimal numValue = System.Convert.ToDecimal(value);
                decimal compareValue = System.Convert.ToDecimal(parameter);
                return numValue > compareValue;
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
