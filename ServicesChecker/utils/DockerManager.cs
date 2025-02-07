using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace ServicesChecker.utils
{
    public class DockerManager
    {
        private DockerClient client;
        private const string ConfigFilePath = "config.json";
        private List<string> containerIds;

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
                containerIds = config.ContainerIds;
            }
            else
            {
                containerIds = new List<string>();
            }
        }

        public async Task<List<string>> GetContainerNamesAsync()
        {
            try
            {
                var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
                return containers
                    .Where(container => containerIds.Contains(container.ID))
                    .Select(container => container.Names.FirstOrDefault()?.TrimStart('/'))
                    .ToList();
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
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });

            foreach (var container in containers)
            {
                if (container.Names.Any(name => name.Contains(selectedContainer)))
                {
                    if (container.State != "running")
                    {
                        await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                    }
                }
                else
                {
                    if (container.State == "running")
                    {
                        await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                    }
                }
            }
        }
    }

    public class Config
    {
        public List<string> ContainerIds { get; set; }
    }
}
