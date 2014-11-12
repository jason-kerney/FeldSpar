using System;
using System.Globalization;
using System.Windows.Data;
using FeldSparGuiCSharp.StyleConstants;
using ViewModel;

namespace FeldSparGuiCSharp.ValueConverters
{
    public class TestStatusColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (TestStatus) value;

            switch (status)
            {
                case TestStatus.Success:
                    return TestStatusColors.SuccessBrush;
                case TestStatus.Running:
                    return TestStatusColors.RunningBrush;
                case TestStatus.Ignored:
                    return TestStatusColors.IgnoredBrush;
                case TestStatus.Failure:
                    return TestStatusColors.FailureBrush;
                default:
                    return TestStatusColors.NoneBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}