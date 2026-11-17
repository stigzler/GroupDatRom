using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace GroupDatRom.Converters
{
    internal class ZeroToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            // Handle Booleans directly
            if (value is bool b) return b;

            // Handle Strings (returns false if null or whitespace/empty)
            if (value is string s) return !string.IsNullOrWhiteSpace(s);

            // Handle Numbers (returns false if 0, true otherwise)
            if (value is int i) return i != 0;
            if (value is long l) return l != 0;
            if (value is double d) return d != 0.0;
            if (value is decimal dec) return dec != 0m;

            // Handle Collections (returns false if empty)
            if (value is ICollection collection) return collection.Count > 0;

            // Fallback for general objects
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
