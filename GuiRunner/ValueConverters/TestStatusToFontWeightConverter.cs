using System;
using System.Globalization;
using System.Windows.Data;
using FeldSpar.Api.Engine.ClrInterop.ViewModels;
using FeldSparGuiCSharp.StyleConstants;

namespace FeldSparGuiCSharp.ValueConverters
{
    public class TestStatusToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (TestStatus) value;

            switch (status)
            {
                case TestStatus.Failure:
                    return TestStatusFontWeight.FailedFontWeight;
                case TestStatus.Ignored:
                    return TestStatusFontWeight.IgnoredFontWeight;
                case TestStatus.Running:
                    return TestStatusFontWeight.RunningFontWeight;
                case TestStatus.Success:
                    return TestStatusFontWeight.SuccessFontWeight;
                default:
                    return TestStatusFontWeight.NoneFontWeight;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}