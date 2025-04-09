using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ServicesChecker.utils
{
    public class LogManager
    {
        private const string LogFilesJsonPath = "logfiles.json";

        public ObservableCollection<LogFileInfo> LoadLogFiles()
        {
            if (File.Exists(LogFilesJsonPath))
            {
                var json = File.ReadAllText(LogFilesJsonPath);
                return JsonConvert.DeserializeObject<ObservableCollection<LogFileInfo>>(json) 
                    ?? new ObservableCollection<LogFileInfo>();
            }
            return new ObservableCollection<LogFileInfo>();
        }

        public void SaveLogFiles(ObservableCollection<LogFileInfo> logFiles)
        {
            var json = JsonConvert.SerializeObject(logFiles);
            File.WriteAllText(LogFilesJsonPath, json);
        }

        public void AddLogFile(ObservableCollection<LogFileInfo> logFiles, string filePath)
        {
            if (!LogFileExists(logFiles, filePath))
            {
                var fileInfo = new FileInfo(filePath);
                logFiles.Add(new LogFileInfo
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    FileSize = FormatFileSize(fileInfo.Length),
                    LastModified = fileInfo.LastWriteTime
                });
                SaveLogFiles(logFiles); // Save changes after adding the file
            }
        }

        public async Task<bool> DeleteLogFileAsync(ObservableCollection<LogFileInfo> logFiles, LogFileInfo logFile)
        {
            if (logFile == null || !File.Exists(logFile.FilePath))
                return false;

            try
            {
                // Attempt to delete the file
                await Task.Run(() => File.Delete(logFile.FilePath));

                // If deletion was successful, remove from collection
                logFiles.Remove(logFile);
                SaveLogFiles(logFiles);
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting log file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool LogFileExists(ObservableCollection<LogFileInfo> logFiles, string filePath)
        {
            foreach (var logFile in logFiles)
            {
                if (logFile.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblBytes = bytes;

            while (dblBytes >= 1024 && i < suffixes.Length - 1)
            {
                dblBytes /= 1024;
                i++;
            }

            return $"{dblBytes:0.##} {suffixes[i]}";
        }
    }

    public class LogFileInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public DateTime LastModified { get; set; }
    }
}
