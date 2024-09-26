using System.ServiceProcess;

namespace ServicesChecker
{
    public class LocalServiceChecker
    {
        // Method to get the status of a specific service by name
        public string CheckServiceStatus(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    return $"Service {serviceName} is {serviceController.Status}";
                }
            }
            catch (InvalidOperationException e)
            {
                return $"Error: {e.Message}";
            }
        }
    }
}
