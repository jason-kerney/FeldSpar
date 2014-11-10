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
                    return Brushes.ForestGreen;
                case TestStatus.Running:
                    return Brushes.DodgerBlue;
                case TestStatus.Ignored:
                    return Brushes.Goldenrod;
                case TestStatus.Failure:
                    return Brushes.Red;
                default:
                    return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}