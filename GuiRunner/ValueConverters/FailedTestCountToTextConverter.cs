using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using FeldSpar.ClrInterop;
using FeldSparGuiCSharp.VeiwModels;

namespace FeldSparGuiCSharp.ValueConverters
{
    public class FailedTestCountToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var assemblyModel = (TestAssemblyModel) value;

            var testDetailModels =
                from test in assemblyModel.Tests
                where test.Status == TestStatus.Failure
                select 1;
            return testDetailModels.Count().ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}