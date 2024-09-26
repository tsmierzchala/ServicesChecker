using System.Collections.ObjectModel;
using System.IO;
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
            if (!string.IsNullOrWhiteSpace(serviceName) && !ServiceExists(serviceName))
            {
                serviceStatuses.Add(new ServiceStatus { Name = serviceName, Status = "Checking..." });
                SaveServiceStatuses();
                UpdateServiceStatuses();
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

        private void StartServiceCheckTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => UpdateServiceStatuses();
            timer.Start();
        }

        private void UpdateServiceStatuses()
        {
            foreach (var service in serviceStatuses)
            {
                var status = CheckServiceStatus(service.Name);
                service.Status = status;
            }
            ServiceStatusListView.Items.Refresh();
        }

        private string CheckServiceStatus(string serviceName)
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