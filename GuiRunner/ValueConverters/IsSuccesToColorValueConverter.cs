using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiRunner.ValueConverters
{
    public class IsSuccesToColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var results = (IEnumerable<FeldSpar.Framework.TestResult>) value;


            if (results.Any(x => x.IsFailure))
            {
                return Brushes.Red;
            }

            return Brushes.Green;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}