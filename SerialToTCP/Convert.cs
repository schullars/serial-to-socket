using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// add by schullar
using System.Windows.Media;

namespace SerialToTCP
{
    class ComboboxConvert:System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value) { return null; }

            var type = value.GetType();

            if (null == type)
            {
                return null;
            }

            if (null == value[0]) { value[0] = value[1]; return value[1]; }
            else { return value[0]; }
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        { return null; }
    }

    class EnableControlConvert : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null == value) { return null; }

            var type = value.GetType();

            if (null == type)
            {
                return null;
            }

            var bgcolor = value as SolidColorBrush;
            if (null == bgcolor) { return null; }

            if(bgcolor.Color == Colors.DodgerBlue)
            {
                return true;
            }
            else { return false; }
           
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { return null; }
    }
}
