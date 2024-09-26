using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Threading;

namespace ServicesChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<ServiceStatus> serviceStatuses;
        private const string JsonFilePath = "services.json";
        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            serviceStatuses = new ObservableCollection<ServiceStatus>();
            ServiceStatusListView.ItemsSource = serviceStatuses;
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = ServiceNameTextBox.Text;
            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                serviceStatuses.Add(new ServiceStatus { Name = serviceName, Status = "Checking..." });
                UpdateServiceStatuses();
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateServiceStatuses();
        }
        private void UpdateServiceStatuses()
        {
            foreach (var serviceStatus in serviceStatuses)
            {
                var newStatus = CheckServiceStatus(serviceStatus.Name);
                serviceStatus.Status = newStatus;
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
            catch (InvalidOperationException e)
            {
                return $"Error: {e.Message}";
            }
        }
    }
}