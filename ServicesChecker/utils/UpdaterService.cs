using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ServicesChecker.utils
{
    public class UpdaterService
    {
        private readonly GitHubApiService _githubApiService;
        private readonly string _updateTempDirectory;
        
        public string CurrentVersion { get; }
        
        public UpdaterService(string owner, string repo)
        {
            _githubApiService = new GitHubApiService(owner, repo);
            
            // Get the current version from the executing assembly
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            
            // Set up the temp directory for updates
            _updateTempDirectory = Path.Combine(
                Path.GetTempPath(),
                "ServicesChecker", 
                "Updates");
            
            // Ensure the temp directory exists
            if (!Directory.Exists(_updateTempDirectory))
                Directory.CreateDirectory(_updateTempDirectory);
        }
        
        public async Task<ReleaseInfo> CheckForUpdatesAsync()
        {
            var latestRelease = await _githubApiService.GetLatestReleaseAsync();
            
            if (latestRelease == null)
                return null;
            
            // Clean version strings for comparison (remove 'v' prefix if present)
            var currentVersionCleaned = CurrentVersion.TrimStart('v');
            var latestVersionCleaned = latestRelease.Version.TrimStart('v');
            
            // Compare versions to see if update is available
            try
            {
                var current = new Version(currentVersionCleaned);
                var latest = new Version(latestVersionCleaned);
                
                if (latest > current)
                {
                    return latestRelease;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error comparing versions: {ex.Message}");
                // If version comparison fails, just return the latest release info
                return latestRelease;
            }
            
            return null; // No update available
        }
        
        public async Task<string> DownloadUpdateAsync(string downloadUrl, Action<double> progressCallback)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // Determine the filename from the URL
                    string fileName = Path.GetFileName(downloadUrl);
                    string downloadPath = Path.Combine(_updateTempDirectory, fileName);
                    
                    // Download the file with progress reporting
                    using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var buffer = new byte[8192];
                        var bytesRead = 0;
                        double totalBytesRead = 0;
                        
                        using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesRead += bytesRead;
                                
                                if (totalBytes > 0)
                                {
                                    double progressPercentage = totalBytesRead / totalBytes * 100;
                                    progressCallback?.Invoke(progressPercentage);
                                }
                            }
                        }
                    }
                    
                    return downloadPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading update: {ex.Message}");
                    throw;
                }
            }
        }
        
        public void InstallUpdate(string downloadedFilePath)
        {
            try
            {
                if (downloadedFilePath.EndsWith(".exe"))
                {
                    // Launch the installer and exit the current app
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = downloadedFilePath,
                        UseShellExecute = true
                    });
                    
                    Application.Current.Shutdown();
                }
                else if (downloadedFilePath.EndsWith(".zip"))
                {
                    // For zip files, create a batch file to:
                    // 1. Kill current process
                    // 2. Extract zip
                    // 3. Restart application
                    string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                    string batchFilePath = Path.Combine(_updateTempDirectory, "update.bat");
                    int currentProcessId = Process.GetCurrentProcess().Id;
                    
                    string batchFileContent = $@"
@echo off
echo Waiting for application to close...
timeout /t 2 /nobreak > NUL
taskkill /F /PID {currentProcessId} > NUL 2>&1

echo Extracting update...
powershell -Command ""Expand-Archive -Path '{downloadedFilePath}' -DestinationPath '{currentDirectory}' -Force""

echo Starting application...
start """" ""{currentExePath}""

echo Cleaning up...
del ""{downloadedFilePath}""
del ""%~f0""
";

                    File.WriteAllText(batchFilePath, batchFileContent);
                    
                    // Execute the batch file
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchFilePath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                    
                    // The batch file will kill this process
                }
                else
                {
                    MessageBox.Show("Unsupported update file format.", "Update Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing update: {ex.Message}", "Update Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
