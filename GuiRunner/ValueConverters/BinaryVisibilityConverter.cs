using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FeldSparGuiCSharp.ValueConverters
{
    public class BinaryVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool) value;
            return v ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}