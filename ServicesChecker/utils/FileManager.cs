using System.IO.Compression;
using System.IO;
using System.Windows;

namespace ServicesChecker.utils
{
    public class FileManager
    {
        public static void CleanDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true); // Delete the directory and all subdirectories/files
                }
                Directory.CreateDirectory(directoryPath); // Recreate the directory
                Console.WriteLine($"Directory {directoryPath} cleaned.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning directory: {ex.Message}");
            }
        }

        public static void CopyFilesWithSetupInName(
            string sourceDirectory,
            string destinationDirectory
        )
        {
            try
            {
                // Validate source directory
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    Console.WriteLine("Source directory is null or empty.");
                    return;
                }

                if (!Directory.Exists(sourceDirectory))
                {
                    Console.WriteLine($"Source directory does not exist: {sourceDirectory}");
                    return;
                }

                // Validate destination directory
                if (string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Console.WriteLine("Destination directory is null or empty.");
                    return;
                }

                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                    Console.WriteLine($"Created destination directory: {destinationDirectory}");
                }

                // Use search pattern to get files containing 'setup' in the name
                string[] files = Directory.GetFiles(
                    sourceDirectory,
                    "*setup*",
                    SearchOption.TopDirectoryOnly
                );

                if (files.Length == 0)
                {
                    Console.WriteLine("No files with 'setup' in the name were found.");
                    return;
                }

                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationFilePath = Path.Combine(destinationDirectory, fileName);

                    try
                    {
                        File.Copy(filePath, destinationFilePath, overwrite: true);
                        Console.WriteLine($"Copied '{fileName}' to '{destinationFilePath}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error copying file '{fileName}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while copying files: {ex.Message}");
            }
        }

        public static void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                // Ensure the source file exists
                if (!File.Exists(sourceFilePath))
                {
                    Console.WriteLine($"Source file does not exist: {sourceFilePath}");
                    return;
                }

                // Create the destination directory if it doesn't exist
                var destinationDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                    Console.WriteLine($"Created destination directory: {destinationDirectory}");
                }

                // Copy the file
                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
                Console.WriteLine($"File copied from {sourceFilePath} to {destinationFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while copying file: {ex.Message}");
            }
        }

        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            try
            {
                // Create all of the directories in the target path
                foreach (
                    var dirPath in Directory.GetDirectories(
                        sourceDir,
                        "*",
                        SearchOption.AllDirectories
                    )
                )
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));
                }

                // Copy all the files to the new directory
                foreach (
                    var newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
                )
                {
                    File.Copy(newPath, newPath.Replace(sourceDir, targetDir), true);
                }

                Console.WriteLine($"Directory copied from {sourceDir} to {targetDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while copying directory: {ex.Message}");
            }
        }

        public static void UnzipFile(string zipFilePath, string destDir)
        {
            try
            {
                // Ensure the destination directory exists
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // Extract the zip file into the destination directory
                ZipFile.ExtractToDirectory(zipFilePath, destDir, true); // Set to overwrite existing files

                Console.WriteLine($"File unzipped successfully to {destDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unzipping file: {ex.Message}");
            }
        }

        public static async Task<string> FindNewestFolder(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException(
                        $"The specified directory does not exist: {directoryPath}"
                    );
                }

                var directories = Directory.GetDirectories(directoryPath);
                if (directories.Length == 0)
                {
                    throw new Exception("No directories found in the specified path.");
                }

                // Find the directory with the most recent modification date
                var newestDirectory =
                    directories
                        .Select(dir => new DirectoryInfo(dir))
                        .OrderByDescending(d => d.LastWriteTime)
                        .FirstOrDefault()
                    ?? throw new Exception("Unable to determine the newest directory.");
                return await Task.FromResult(newestDirectory.FullName);
            }
            catch (DirectoryNotFoundException dirEx)
            {
                Console.WriteLine($"Directory error: {dirEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding newest folder: {ex.Message}");
                throw;
            }
        }

        public static async Task GetAppsAsync(Action<double, string> updateProgress)
        {
            try
            {
                string targetDirectory = @"D:\Aplikacje\Temp\";
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var directoriesToDelete = new[] { "Market3_Ross", "Market3_IM", "Market3_BM", "Forsmann", "Prommann" };
                foreach (var dir in directoriesToDelete)
                {
                    DeleteDirectoryIfExists(Path.Combine(targetDirectory, dir));
                }

                updateProgress(0, "Mapping network drives...");
                NetworkDriveManager.MapNetworkDrive(
                    "K",
                    @"\\10.60.1.242\m3build",
                    @"dforcom\dpaczkowski",
                    "koza1500"
                );
                NetworkDriveManager.MapNetworkDrive(
                    "G",
                    @"\\synek\serwis",
                    @"dforcom\dpaczkowski",
                    "koza1500"
                );

                updateProgress(5, "Loading configuration...");
                string addressIP = @"\\10.60.1.242\M3Build";
                string forsmanDir = @"\\synek\serwis\Forsmann\Forsmann_Develop\";
                string marketRossmannDir = $@"{addressIP}\Rossmann\";
                string marketStoreDir = $@"{addressIP}\RossmannStore\";
                string marketIMDir = @"\\SYNEK\M3Build\Musketeers_Intermarche\";
                string marketBMDir = @"\\SYNEK\M3Build\Musketeers_Bricomarche\";

                string newestForsmannDir = Directory.GetDirectories(forsmanDir).OrderBy(d => new DirectoryInfo(d).CreationTime).Last();
                string newestForsmannVersion = Path.GetFileName(newestForsmannDir);
                string newestMarketIMZip = Directory.GetFiles(marketIMDir, "*.zip").OrderBy(f => new FileInfo(f).CreationTime).Last();
                string newestMarketBMZip = Directory.GetFiles(marketBMDir, "*.zip").OrderBy(f => new FileInfo(f).CreationTime).Last();
                string newestMarketStoreZip = Directory.GetFiles(marketStoreDir, "*.zip").OrderBy(f => new FileInfo(f).CreationTime).Last();

                var tasks = new List<Task>
                {
                    Task.Run(async () => CopyDirectory((string?)await FindNewestFolder(forsmanDir), targetDirectory)),
                    Task.Run(() => File.Copy(newestMarketIMZip, Path.Combine(targetDirectory, Path.GetFileName(newestMarketIMZip)), true)),
                    Task.Run(() => File.Copy(newestMarketBMZip, Path.Combine(targetDirectory, Path.GetFileName(newestMarketBMZip)), true)),
                    Task.Run(() => File.Copy(newestMarketStoreZip, Path.Combine(targetDirectory, Path.GetFileName(newestMarketStoreZip)), true))
                };
                updateProgress(10, "Downloading files...");
                await Task.WhenAll(tasks);

                // Wypakowywanie plików
                tasks = new List<Task>
                {
                    Task.Run(() =>
                    {
                        Directory.CreateDirectory(Path.Combine(targetDirectory, "Market3_IM"));
                        ZipFile.ExtractToDirectory(Path.Combine(targetDirectory, Path.GetFileName(newestMarketIMZip)), Path.Combine(targetDirectory, "Market3_IM"));
                    }),
                    Task.Run(() =>
                    {
                        Directory.CreateDirectory(Path.Combine(targetDirectory, "Market3_BM"));
                        ZipFile.ExtractToDirectory(Path.Combine(targetDirectory, Path.GetFileName(newestMarketBMZip)), Path.Combine(targetDirectory, "Market3_BM"));
                    }),
                    Task.Run(() =>
                    {
                        Directory.CreateDirectory(Path.Combine(targetDirectory, "Market3_Ross"));
                        ZipFile.ExtractToDirectory(Path.Combine(targetDirectory, Path.GetFileName(newestMarketStoreZip)), Path.Combine(targetDirectory, "Market3_Ross"));
                    }),
                    Task.Run(() =>
                    {
                        Directory.CreateDirectory(Path.Combine(targetDirectory, "Forsmann"));
                        ZipFile.ExtractToDirectory(Path.Combine(targetDirectory, $"Forsmann_{newestForsmannVersion}.zip"), Path.Combine(targetDirectory, "Forsmann"));
                    }),
                    Task.Run(() =>
                    {
                        Directory.CreateDirectory(Path.Combine(targetDirectory, "Prommann"));
                        ZipFile.ExtractToDirectory(Path.Combine(targetDirectory, $"Prommann_{newestForsmannVersion}.zip"), Path.Combine(targetDirectory, "Prommann"));
                    })
                };
                updateProgress(40, "Unpacking files...");
                await Task.WhenAll(tasks);
 
                updateProgress(80, "Get config M3 and set in Market3 folder...");
                CopyDirectory(
                    @"G:\tsmierzchala\Automaty\Market3\config",
                    Path.Combine(targetDirectory, @"Market3\config")
                );

                updateProgress(90, "Deleting zip files...");
                var zipFiles = Directory.GetFiles(targetDirectory, "*.zip");
                foreach (var zipFile in zipFiles)
                {
                    File.Delete(zipFile);
                }
                updateProgress(100, "Finished.");
                //MessageBox.Show("Nowe wersje zostały prawidłowo pobrane.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas pobierania aplikacji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
