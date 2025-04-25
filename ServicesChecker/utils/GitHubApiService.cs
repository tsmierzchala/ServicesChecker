using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServicesChecker.utils
{
    public class GitHubApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _owner;
        private readonly string _repo;
        
        public GitHubApiService(string owner, string repo)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("ServicesChecker", "1.0"));
            
            _owner = owner;
            _repo = repo;
        }

        public async Task<ReleaseInfo> GetLatestReleaseAsync()
        {
            try
            {
                string apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var releaseData = JsonConvert.DeserializeObject<GithubRelease>(json);
                    
                    return new ReleaseInfo
                    {
                        Version = releaseData.TagName,
                        ReleaseDate = releaseData.PublishedAt,
                        DownloadUrl = GetWindowsAssetDownloadUrl(releaseData),
                        Description = releaseData.Body
                    };
                }
                else
                {
                    Console.WriteLine($"Error fetching releases: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when fetching release info: {ex.Message}");
                return null;
            }
        }
        
        private string GetWindowsAssetDownloadUrl(GithubRelease release)
        {
            // Look for Windows executable or zip file
            foreach (var asset in release.Assets)
            {
                if (asset.Name.EndsWith(".exe") || 
                    asset.Name.Contains("setup") || 
                    (asset.Name.EndsWith(".zip") && asset.Name.Contains("win")))
                {
                    return asset.BrowserDownloadUrl;
                }
            }
            
            // Default to first asset if no Windows-specific asset is found
            return release.Assets.Count > 0 ? release.Assets[0].BrowserDownloadUrl : null;
        }
    }

    public class ReleaseInfo
    {
        public string Version { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string DownloadUrl { get; set; }
        public string Description { get; set; }
    }

    // Classes for deserializing GitHub API response
    public class GithubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }
        
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
        
        [JsonProperty("assets")]
        public List<GithubAsset> Assets { get; set; }
        
        [JsonProperty("body")]
        public string Body { get; set; }
    }

    public class GithubAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
