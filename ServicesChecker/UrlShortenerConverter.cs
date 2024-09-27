using System.Globalization;
using System.Windows.Data;

namespace ServicesChecker
{
    public class UrlShortenerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url)
            {
                // Shorten URL to a maximum of 30 characters for display, you can customize this
                int maxLength = 30;
                if (url.Length > maxLength)
                {
                    return url.Substring(0, maxLength - 3) + "...";
                }
                return url;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
