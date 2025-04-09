using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace ServicesChecker.utils
{
    public class DockerManager
    {
        private DockerClient client;
        private const string ConfigFilePath = "config.json";
        private List<string> containerNames;

        public void InitializeDockerClient()
        {
            var dockerUri = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock";
            client = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
        }

        public DockerClient GetClient()
        {
            return client;
        }

        public void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonConvert.DeserializeObject<Config>(json);
                containerNames = config.ContainerNames;
                Console.WriteLine("Loaded container names from config: " + string.Join(", ", containerNames));
            }
            else
            {
                containerNames = new List<string>();
                Console.WriteLine("Config file not found, using empty container name list.");
            }
        }

        public async Task<bool> IsDockerDaemonRunningAsync()
        {
            try
            {
                // Wykonujemy prostą operację, aby sprawdzić, czy Docker daemon odpowiada
                await client.System.PingAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Docker daemon is not running or unreachable: {ex.Message}");
                return false;
            }
        }


        public async Task<List<string>> GetContainerNamesAsync()
        {
            try
            {
                // Sprawdzamy, czy Docker daemon działa
                if (!await IsDockerDaemonRunningAsync())
                {
                    MessageBox.Show("Docker daemon is not running. Please start Docker and try again.", "Docker Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // close app after closed message box
                    Application.Current.Shutdown();

                    return new List<string>();
                }

                // Pobieramy listę kontenerów
                var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                var filteredContainers = containers
                    .Where(container => containerNames.Contains(container.Names.FirstOrDefault()?.TrimStart('/')))
                    .Select(container => container.Names.FirstOrDefault()?.TrimStart('/'))
                    .ToList();

                Console.WriteLine("Filtered container names: " + string.Join(", ", filteredContainers));
                return filteredContainers;
            }
            catch (HttpRequestException ex)
            {
                // Log the exception and handle it appropriately
                Console.WriteLine($"Error connecting to Docker: {ex.Message}");
                throw;
            }
        }

        public async Task<ContainerListResponse> GetContainerByNameAsync(string name)
        {
            try
            {
                var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                return containers.FirstOrDefault(container => container.Names.Any(n => n.TrimStart('/') == name));
            }
            catch (HttpRequestException ex)
            {
                // Log the exception and handle it appropriately
                Console.WriteLine($"Error connecting to Docker: {ex.Message}");
                throw;
            }
        }

        public static async Task ManageDockerContainers(string selectedContainer, DockerClient client)
        {
            try
            {
                var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });

                foreach (var container in containers)
                {
                    Console.WriteLine($"Checking container: {container.Names.FirstOrDefault()} with state: {container.State}");

                    if (container.Names.Any(name => name.Equals(selectedContainer)))
                    {
                        if (container.State != "running")
                        {
                            Console.WriteLine($"Starting container: {container.ID}");
                            bool started = await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                            if (!started)
                            {
                                Console.WriteLine($"Failed to start container: {container.ID}");
                            }
                        }
                    }
                    else
                    {
                        if (container.State == "running")
                        {
                            Console.WriteLine($"Stopping container: {container.ID}");
                            bool stopped = await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                            if (!stopped)
                            {
                                Console.WriteLine($"Failed to stop container: {container.ID}");
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error connecting to Docker: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

    }

    public class Config
    {
        public List<string> ContainerNames { get; set; }
    }
}
