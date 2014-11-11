using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GuiRunner.StyleConstants;
using ViewModel;

namespace GuiRunner.ValueConverters
{
    public class TestAssemblyStatusColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tests = (IEnumerable<TestDetailModel>) value;

            var statuses = (
                from testDetailModel in tests
                select testDetailModel.Status
                ).Distinct().ToList();


            if (statuses.Any(tst => tst == TestStatus.Failure))
            {
                return TestStatusColors.FailureBrush;
            }

            if (statuses.Any(tst => tst == TestStatus.Ignored))
            {
                return TestStatusColors.IgnoredBrush;
            }

            if (statuses.Any(tst => tst == TestStatus.Success))
            {
                return TestStatusColors.SuccessBrush;
            }

            if (statuses.Any(tst => tst == TestStatus.Running))
            {
                return TestStatusColors.RunningBrush;
            }

            return TestStatusColors.NoneBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}