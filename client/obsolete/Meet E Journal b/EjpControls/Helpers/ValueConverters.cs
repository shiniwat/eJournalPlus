using System;
using System.Windows.Data;

namespace SiliconStudio.Meet.EjpControls.Helpers
{
    class ZoomToString : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Double z = Double.Parse(value.ToString());
            int sz = (int)z;
            return sz.ToString() + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
