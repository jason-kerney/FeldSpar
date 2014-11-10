using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using ViewModel;

namespace GuiRunner.ValueConverters
{
    public class TestAssemblyStatusColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (IEnumerable<TestDetailModel>) value;

            if (status.Any(tst => tst.Status == TestStatus.Failure))
            {
                return Brushes.Red;
            }

            if (status.Any(tst => tst.Status == TestStatus.Ignored))
            {
                return Brushes.Gold;
            }

            if (status.Any(tst => tst.Status == TestStatus.Success))
            {
                return Brushes.GreenYellow;
            }

            if (status.Any(tst => tst.Status == TestStatus.Running))
            {
                return Brushes.DodgerBlue;
            }

            return Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}