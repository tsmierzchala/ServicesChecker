using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Data;
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
            AddSorting();
        }
        private async void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = ServiceNameTextBox.Text;
            if (!string.IsNullOrWhiteSpace(serviceName) && !ServiceExists(serviceName))
            {
                if (CheckIfLocalServiceExists(serviceName))
                {
                    AddService(serviceName, false, "Local Service");
                }
                else if (await CheckIfRestServiceExists(serviceName))
                {
                    AddService(serviceName, true, "REST Service");
                }
                else
                {
                    MessageBox.Show("The specified service does not exist as either a local or REST service.", "Service Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            ServiceNameTextBox.Clear();
        }
        private bool CheckIfLocalServiceExists(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    var status = serviceController.Status;
                    return true; // If no exception is thrown, the service exists
                }
            }
            catch
            {
                return false; // Service does not exist
            }
        }
        private async Task<bool> CheckIfRestServiceExists(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false; // Address is not a valid REST service
            }
        }

        private void AddService(string serviceName, bool isRestService, string status)
        {
            serviceStatuses.Add(new ServiceStatus
            {
                Name = serviceName,
                Status = status,
                IsRestService = isRestService
            });
            SaveServiceStatuses();
            UpdateServiceStatuses();
        }
        private async void StartServiceCheckTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += async (s, e) => await UpdateServiceStatuses();
            timer.Start();
        }
        private async Task UpdateServiceStatuses()
        {
            var serviceStatusesCopy = serviceStatuses.ToList();
            foreach (var service in serviceStatusesCopy)
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
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

            if (ServiceStatusListView.SelectedItem is ServiceStatus selectedService)
            {
                serviceStatuses.Remove(selectedService);
                SaveServiceStatuses();
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

        private void AddSorting()
        {
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(serviceStatuses);
            collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            collectionView.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Ascending));
        }
    }
}