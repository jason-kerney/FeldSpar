using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ViewModel;

namespace GuiRunner.ValueConverters
{
    public class TestStatusColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (TestStatus) value;

            switch (status)
            {
                case TestStatus.Success:
                    return Brushes.GreenYellow;
                case TestStatus.Running:
                    return Brushes.DodgerBlue;
                case TestStatus.Ignored:
                    return Brushes.Khaki;
                case TestStatus.Failure:
                    return Brushes.Red;
                default:
                    return Brushes.LightGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}