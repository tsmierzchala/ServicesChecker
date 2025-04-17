using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Docker.DotNet.Models;
using ServicesChecker.utils;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Globalization;
using System.Linq;

namespace ServicesChecker
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ServiceStatus> serviceStatuses;
        private ObservableCollection<LogFileInfo> logFiles;
        private DispatcherTimer timer;
        private DispatcherTimer logFileMonitorTimer;
        private DockerManager dockerManager;
        private ServiceManager serviceManager;
        private LogManager logManager;
        private string currentContainer = null;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services manager
            serviceManager = new ServiceManager();
            serviceStatuses = serviceManager.LoadServiceStatuses();
            ServiceStatusListView.ItemsSource = serviceStatuses;

            // Initialize log files manager
            logManager = new LogManager();
            logFiles = logManager.LoadLogFiles();
            LogFilesListView.ItemsSource = logFiles;

            StartServiceCheckTimer();
            StartLogFileMonitorTimer();
            AddSorting();
            InitializeDockerManager();
        }

        #region Services Tab Methods
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

            ComboBoxItem selectedItem = null;

            foreach (var name in containerNames)
            {
                var item = new ComboBoxItem
                {
                    Content = name
                };
                DockerContainerComboBox.Items.Add(item);

                // Sprawdź, czy kontener jest uruchomiony
                var container = await dockerManager.GetContainerByNameAsync(name);
                if (container != null && container.State == "running" && selectedItem == null)
                {
                    selectedItem = item;
                }
            }

            // Ustaw uruchomiony kontener jako wybrany
            if (selectedItem != null)
            {
                DockerContainerComboBox.SelectedItem = selectedItem;
            }
        }

        private async void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            string serviceName = ServiceNameTextBox.Text;
            bool isConnectingToDB = IsConnectingToDBCheckBox.IsChecked == true;

            if (!string.IsNullOrWhiteSpace(serviceName) && !serviceManager.ServiceExists(serviceStatuses, serviceName))
            {
                if (serviceManager.CheckIfLocalServiceExists(serviceName))
                {
                    serviceManager.AddService(serviceStatuses, serviceName, false, "Local Service", isConnectingToDB);
                }
                else if (await serviceManager.CheckIfRestServiceExists(serviceName))
                {
                    serviceManager.AddService(serviceStatuses, serviceName, true, "REST Service", isConnectingToDB);
                }
                else
                {
                    MessageBox.Show("The specified service does not exist as either a local or REST service.", "Service Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            ServiceNameTextBox.Clear();
            IsConnectingToDBCheckBox.IsChecked = false;
        }

        private async void StartServiceCheckTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
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
            // Services sorting
            ICollectionView servicesView = CollectionViewSource.GetDefaultView(serviceStatuses);
            servicesView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            servicesView.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Ascending));

            // Log files sorting
            ICollectionView logFilesView = CollectionViewSource.GetDefaultView(logFiles);
            logFilesView.SortDescriptions.Add(new SortDescription("FileName", ListSortDirection.Ascending));
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

        private async void RestartServiceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ServiceStatusListView.SelectedItem is ServiceStatus selectedService && !selectedService.IsRestService)
            {
                try
                {
                    await serviceManager.RestartLocalServiceAsync(selectedService.Name);
                    serviceManager.SaveServiceStatuses(serviceStatuses);
                    await UpdateServiceStatuses();
                    ServiceStatusListView.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error restarting service: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ServiceStatusListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (ServiceStatusListView.SelectedItem is ServiceStatus selectedService)
            {
                // Find the context menu and the specific menu items
                ContextMenu contextMenu = ServiceStatusListView.ContextMenu;

                MenuItem changeStatusMenuItem = contextMenu.Items
                    .OfType<MenuItem>()
                    .FirstOrDefault(item => item.Header.ToString() == "Change status");

                MenuItem restartMenuItem = contextMenu.Items
                    .OfType<MenuItem>()
                    .FirstOrDefault(item => item.Header.ToString() == "Restart");

                if (changeStatusMenuItem != null)
                {
                    // Set visibility based on whether the service is a REST service or not
                    changeStatusMenuItem.Visibility = selectedService.IsRestService ? Visibility.Collapsed : Visibility.Visible;
                }

                if (restartMenuItem != null)
                {
                    // Only show restart option for local services
                    restartMenuItem.Visibility = selectedService.IsRestService ? Visibility.Collapsed : Visibility.Visible;
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
                string selectedContainer = "/" + selectedItem.Content.ToString();

                if (currentContainer != null && currentContainer != selectedContainer)
                {
                    // Najpierw zatrzymaj usługi łączące się z bazą danych
                    foreach (var service in serviceStatuses.Where(s => s.IsConnectingToDB))
                    {
                        if (serviceManager.CheckIfLocalServiceExists(service.Name))
                        {
                            await serviceManager.StopService(service.Name);
                        }
                    }

                    // Następnie zatrzymaj aktualny kontener
                    var currentContainerInfo = await dockerManager.GetContainerByNameAsync(currentContainer.TrimStart('/'));
                    if (currentContainerInfo != null && currentContainerInfo.State == "running")
                    {
                        bool stopped = await dockerManager.GetClient().Containers.StopContainerAsync(currentContainerInfo.ID, new ContainerStopParameters());
                        if (!stopped)
                        {
                            Console.WriteLine($"Failed to stop container: {currentContainerInfo.ID}");
                            return; // Jeśli nie uda się zatrzymać, zakończ operację
                        }
                    }

                    // Ustaw nowy kontener jako aktualny
                    currentContainer = selectedContainer;
                }

                // Uruchom nowo wybrany kontener
                await DockerManager.ManageDockerContainers(selectedContainer, dockerManager.GetClient());
                currentContainer = selectedContainer;
            }
        }
        #endregion

        #region Configuration Tab Methods
        private void BrowseLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Select Log Files",
                Multiselect = true // Allow multiple file selection
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    if (!logManager.LogFileExists(logFiles, filePath))
                    {
                        logManager.AddLogFile(logFiles, filePath);
                    }
                }
                LogFilesListView.Items.Refresh(); // Refresh the list view to show the added files
            }
        }

        private async void DeleteLogFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (LogFilesListView.SelectedItem is LogFileInfo selectedLogFile)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the file '{selectedLogFile.FileName}'?\nThis will permanently remove the file from disk.",
                    "Confirm Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (await logManager.DeleteLogFileAsync(logFiles, selectedLogFile))
                    {
                        MessageBox.Show("File successfully deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LogFilesListView.Items.Refresh();
                    }
                }
            }
        }

        private async void DeleteLogFilesFromDiskButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete all log files from disk?\n" +
                "The files will be removed from your computer but kept in the application for monitoring.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteLogFilesFromDiskButton.IsEnabled = false;
                try
                {
                    int deletedCount = await logManager.DeleteFilesFromDiskAsync(logFiles);

                    if (deletedCount > 0)
                    {
                        MessageBox.Show(
                            $"{deletedCount} log file(s) successfully deleted from disk but kept in the application for monitoring.",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        LogFilesListView.Items.Refresh();
                    }
                    else
                    {
                        MessageBox.Show(
                            "No files were deleted. All files may have already been removed from disk.",
                            "Information",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                finally
                {
                    DeleteLogFilesFromDiskButton.IsEnabled = true;
                }
            }
        }

        private void OpenLogLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (LogFilesListView.SelectedItem is LogFileInfo selectedLogFile &&
                File.Exists(selectedLogFile.FilePath))
            {
                if (!selectedLogFile.FileExists)
                {
                    MessageBox.Show(
                        "This file does not exist at the specified location.",
                        "File Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Open the file's directory in Windows Explorer and select the file
                string argument = $"/select, \"{selectedLogFile.FilePath}\"";
                Process.Start("explorer.exe", argument);
            }
        }

        private void StartLogFileMonitorTimer()
        {
            logFileMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            logFileMonitorTimer.Tick += (s, e) =>
            {
                logManager.CheckAllFilesExistence(logFiles);
                logManager.UpdateFileSizes(logFiles);
                LogFilesListView.Items.Refresh();
            };
            logFileMonitorTimer.Start();
        }
        #endregion
    }
}
