using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FelicaSharp
{
    [ValueConversion(typeof(string), typeof(bool))]
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value == null || !bool.TryParse(value.ToString(), out flag))
            {
                return Binding.DoNothing;
            }
            return !flag;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
