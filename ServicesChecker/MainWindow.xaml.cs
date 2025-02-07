using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Docker.DotNet;
using Newtonsoft.Json;
using ServicesChecker.utils;

namespace ServicesChecker
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ServiceStatus> serviceStatuses;
        private DispatcherTimer timer;
        private DockerManager dockerManager;
        private ServiceManager serviceManager;

        public MainWindow()
        {
            InitializeComponent();
            serviceManager = new ServiceManager();
            serviceStatuses = serviceManager.LoadServiceStatuses();
            ServiceStatusListView.ItemsSource = serviceStatuses;
            StartServiceCheckTimer();
            AddSorting();
            InitializeDockerManager();
        }

        private void InitializeDockerManager()
        {
            dockerManager = new DockerManager();
            dockerManager.InitializeDockerClient();
            dockerManager.LoadConfig();
            LoadDockerContainers();
        }

        private async void LoadDockerContainers()
        {
            var containerNames = await dockerManager.GetContainerNamesAsync();
            DockerContainerComboBox.Items.Clear();

            foreach (var name in containerNames)
            {
                DockerContainerComboBox.Items.Add(new ComboBoxItem
                {
                    Content = name
                });
            }
        }

        private async void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = ServiceNameTextBox.Text;
            if (!string.IsNullOrWhiteSpace(serviceName) && !serviceManager.ServiceExists(serviceStatuses, serviceName))
            {
                if (serviceManager.CheckIfLocalServiceExists(serviceName))
                {
                    serviceManager.AddService(serviceStatuses, serviceName, false, "Local Service");
                }
                else if (await serviceManager.CheckIfRestServiceExists(serviceName))
                {
                    serviceManager.AddService(serviceStatuses, serviceName, true, "REST Service");
                }
                else
                {
                    MessageBox.Show("The specified service does not exist as either a local or REST service.", "Service Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            ServiceNameTextBox.Clear();
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
                    service.Status = await serviceManager.CheckRestServiceStatus(service.Name);
                }
                else
                {
                    service.Status = serviceManager.CheckLocalServiceStatus(service.Name);
                }
            }
            ServiceStatusListView.Items.Refresh();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceStatusListView.SelectedItem is ServiceStatus selectedService)
            {
                serviceStatuses.Remove(selectedService);
                serviceManager.SaveServiceStatuses(serviceStatuses);
            }
        }

        private void AddSorting()
        {
            ICollectionView collectionView = CollectionViewSource.GetDefaultView(serviceStatuses);
            collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            collectionView.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Ascending));
        }

        private async void ChangeServiceStatusMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceStatusListView.SelectedItem is ServiceStatus selectedService && !selectedService.IsRestService)
            {
                await serviceManager.ChangeLocalServiceStatusAsync(selectedService.Name);
                serviceManager.SaveServiceStatuses(serviceStatuses);
                await UpdateServiceStatuses();
                ServiceStatusListView.Items.Refresh();
            }
        }

        private void ServiceStatusListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (ServiceStatusListView.SelectedItem is ServiceStatus selectedService)
            {
                // Find the context menu and the specific menu item
                ContextMenu contextMenu = ServiceStatusListView.ContextMenu;
                MenuItem changeStatusMenuItem = contextMenu.Items
                    .OfType<MenuItem>()
                    .FirstOrDefault(item => item.Header.ToString() == "Change status");

                if (changeStatusMenuItem != null)
                {
                    // Set visibility based on whether the service is a REST service or not
                    changeStatusMenuItem.Visibility = selectedService.IsRestService ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }

        private async void GetAppsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetAppsButton.Visibility = Visibility.Collapsed; // Ukryj przycisk
                LoadingProgressBar.Visibility = Visibility.Visible; // Pokaż wskaźnik ładowania
                LoadingStatusTextBlock.Visibility = Visibility.Visible; // Pokaż status ładowania

                await FileManager.GetAppsAsync(UpdateProgress);
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed; // Ukryj wskaźnik ładowania
                LoadingStatusTextBlock.Visibility = Visibility.Collapsed; // Ukryj status ładowania
                GetAppsButton.Visibility = Visibility.Visible; // Pokaż przycisk
            }
        }

        private void UpdateProgress(string status)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingStatusTextBlock.Text = status;
            });
        }

        private async void DockerContainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DockerContainerComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedContainer = selectedItem.Content.ToString();
                await DockerManager.ManageDockerContainers(selectedContainer, dockerManager.GetClient());
            }
        }
    }
}
