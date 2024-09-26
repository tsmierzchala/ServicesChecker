using System.Windows.Media;

namespace ServicesChecker
{
    public class ServiceStatus
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public Brush StatusColor => Status.Contains("Running") ? Brushes.Green : Brushes.Red;
        public bool IsRestService { get; set; }
    }
}
