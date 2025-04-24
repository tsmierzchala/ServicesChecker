using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.ServiceProcess;
using Newtonsoft.Json;

namespace ServicesChecker.utils
{
    public class ServiceManager
    {
        private const string JsonFilePath = "services.json";

        public ObservableCollection<ServiceStatus> LoadServiceStatuses()
        {
            if (File.Exists(JsonFilePath))
            {
                var json = File.ReadAllText(JsonFilePath);
                return JsonConvert.DeserializeObject<ObservableCollection<ServiceStatus>>(json);
            }
            return new ObservableCollection<ServiceStatus>();
        }

        public void SaveServiceStatuses(ObservableCollection<ServiceStatus> serviceStatuses)
        {
            var json = JsonConvert.SerializeObject(serviceStatuses);
            File.WriteAllText(JsonFilePath, json);
        }

        public bool CheckIfLocalServiceExists(string serviceName)
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

        public async Task<bool> CheckIfRestServiceExists(string url)
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

        public string CheckLocalServiceStatus(string serviceName)
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

        public async Task<string> CheckRestServiceStatus(string url)
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

        public bool ServiceExists(ObservableCollection<ServiceStatus> serviceStatuses, string serviceName)
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

        public void AddService(ObservableCollection<ServiceStatus> serviceStatuses, string serviceName, bool isRestService, string status, bool isConnectingToDB)
        {
            string version = "N/A";
            if (!isRestService)
            {
                // Only get version for local services
                version = GetLocalServiceVersion(serviceName);
            }

            serviceStatuses.Add(new ServiceStatus
            {
                Name = serviceName,
                Status = status,
                IsRestService = isRestService,
                IsConnectingToDB = isConnectingToDB,
                Version = version
            });
            SaveServiceStatuses(serviceStatuses);
        }

        public async Task ChangeLocalServiceStatusAsync(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        serviceController.Stop();
                        await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Stopped));
                    }
                    else if (serviceController.Status == ServiceControllerStatus.Stopped)
                    {
                        serviceController.Start();
                        await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Running));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to change service status: {ex.Message}", ex);
            }
        }

        public async Task StopService(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        serviceController.Stop();
                        await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Stopped));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to change service status: {ex.Message}", ex);
            }
        }

        public async Task RestartLocalServiceAsync(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    // Stop the service if it's running
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        serviceController.Stop();
                        await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Stopped));
                    }
                    
                    // Start the service once it's stopped
                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                    {
                        serviceController.Start();
                        await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Running));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restart service: {ex.Message}", ex);
            }
        }

        public string GetLocalServiceVersion(string serviceName)
        {
            try
            {
                // Get the service executable path
                string servicePath = GetServiceExecutablePath(serviceName);
                
                if (!string.IsNullOrEmpty(servicePath) && File.Exists(servicePath))
                {
                    // Get the version info from the executable
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(servicePath);
                    return versionInfo.FileVersion ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                // If there's an error, return "Unknown"
                System.Diagnostics.Debug.WriteLine($"Error getting version: {ex.Message}");
            }
            
            return "Unknown";
        }

        private string GetServiceExecutablePath(string serviceName)
        {
            try
            {
                // Query Windows Management to get the service executable path
                string wmiQuery = $"SELECT PathName FROM Win32_Service WHERE Name='{serviceName}'";
                using (var searcher = new ManagementObjectSearcher(wmiQuery))
                {
                    using (var results = searcher.Get())
                    {
                        foreach (ManagementObject result in results)
                        {
                            string pathName = result["PathName"]?.ToString() ?? string.Empty;
                            
                            // Remove quotes if present
                            if (pathName.StartsWith("\""))
                            {
                                int endQuoteIndex = pathName.IndexOf("\"", 1);
                                if (endQuoteIndex > 0)
                                {
                                    pathName = pathName.Substring(1, endQuoteIndex - 1);
                                }
                            }
                            else
                            {
                                // If no quotes, take everything before first space (if there are parameters)
                                int spaceIndex = pathName.IndexOf(" ");
                                if (spaceIndex > 0)
                                {
                                    pathName = pathName.Substring(0, spaceIndex);
                                }
                            }
                            
                            return pathName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting service path: {ex.Message}");
            }
            
            return string.Empty;
        }
    }
}
