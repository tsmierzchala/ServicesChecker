using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace ServicesChecker
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ServiceStatus> serviceStatuses;
        private const string JsonFilePath = "services.json";
        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            serviceStatuses = LoadServiceStatuses();
            ServiceStatusListView.ItemsSource = serviceStatuses;
            StartServiceCheckTimer();
        }
        private void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = ServiceNameTextBox.Text;
            bool isRestService = IsRestServiceCheckbox.IsChecked == true;

            if (!string.IsNullOrWhiteSpace(serviceName) && !ServiceExists(serviceName))
            {
                serviceStatuses.Add(new ServiceStatus
                {
                    Name = serviceName,
                    Status = "Checking...",
                    IsRestService = isRestService
                });
                SaveServiceStatuses();
                UpdateServiceStatuses();
            }
        }
        private async void StartServiceCheckTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += async (s, e) => await UpdateServiceStatuses();
            timer.Start();
        }
        private async Task UpdateServiceStatuses()
        {
            foreach (var service in serviceStatuses)
            {
                if (service.IsRestService)
                {
                    service.Status = await CheckRestServiceStatus(service.Name);
                }
                else
                {
                    service.Status = CheckLocalServiceStatus(service.Name);
                }
            }
            ServiceStatusListView.Items.Refresh();
        }
        private string CheckLocalServiceStatus(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    return serviceController.Status.ToString();
                }
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
        private async Task<string> CheckRestServiceStatus(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode ? "Available" : "Unavailable";
                }
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
        private bool ServiceExists(string serviceName)
        {
            foreach (var service in serviceStatuses)
            {
                if (service.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        private void SaveServiceStatuses()
        {
            var json = JsonConvert.SerializeObject(serviceStatuses);
            File.WriteAllText(JsonFilePath, json);
        }
        private ObservableCollection<ServiceStatus> LoadServiceStatuses()
        {
            if (File.Exists(JsonFilePath))
            {
                var json = File.ReadAllText(JsonFilePath);
                return JsonConvert.DeserializeObject<ObservableCollection<ServiceStatus>>(json);
            }
            return new ObservableCollection<ServiceStatus>();
        }
    }
}