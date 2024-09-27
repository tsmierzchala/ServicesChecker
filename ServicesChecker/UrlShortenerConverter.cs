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
                try
                {
                    // Create a Uri instance to easily parse the URL
                    var uri = new Uri(url);

                    // Get the host and port part from the URI
                    string hostPart = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

                    // Split the path and take the last segment
                    string[] segments = uri.Segments;
                    string lastSegment = segments.LastOrDefault()?.Trim('/');

                    // Combine the host part with the last path segment
                    string shortenedUrl = string.IsNullOrWhiteSpace(lastSegment)
                        ? hostPart
                        : $"{hostPart}/{lastSegment}";

                    return shortenedUrl;
                }
                catch (UriFormatException)
                {
                    // In case of a malformed URL, fallback to returning the original value
                    return url;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
