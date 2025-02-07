using System.Collections.ObjectModel;
using System.IO;
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
            serviceStatuses.Add(new ServiceStatus
            {
                Name = serviceName,
                Status = status,
                IsRestService = isRestService,
                IsConnectingToDB = isConnectingToDB
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
    }
}
