using System;
using System.Globalization;
using System.Windows.Data;
using FeldSpar.ClrInterop;
using FeldSparGuiCSharp.StyleConstants;
using FeldSparGuiCSharp.VeiwModels;

namespace FeldSparGuiCSharp.ValueConverters
{
    public class TestStatusColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TestStatusColors.GetStatusBrush((TestStatus) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}