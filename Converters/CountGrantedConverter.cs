using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace POS_BIGA.Converters
{
    /// <summary>????? ????? PermissionItem ??? ??? ????????? ???????? ???</summary>
    public class CountGrantedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable items)
            {
                int total = 0, granted = 0;
                foreach (var item in items)
                {
                    total++;
                    var prop = item.GetType().GetProperty("IsGranted");
                    if (prop?.GetValue(item) is true) granted++;
                }
                return $"{granted}/{total}";
            }
            return "0/0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
