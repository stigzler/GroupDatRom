using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GroupDatRom.Converters
{
    public class BoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool b)
            {
                flag = b;
            }
            else if (value is bool?)
            {
                flag = ((bool?)value).GetValueOrDefault();
            }

            // If parameter is set to "Inverse", true becomes Collapsed
            if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                bool result = vis == Visibility.Visible;
                if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    result = !result;
                }
                return result;
            }
            return false;
        }
    }
}