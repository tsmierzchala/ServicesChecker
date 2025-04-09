using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
                var logFiles = JsonConvert.DeserializeObject<ObservableCollection<LogFileInfo>>(json)
                    ?? new ObservableCollection<LogFileInfo>();

                // Update file existence status for all loaded files
                foreach (var logFile in logFiles)
                {
                    logFile.UpdateExistenceStatus();
                }

                return logFiles;
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

        public async Task<int> DeleteFilesFromDiskAsync(ObservableCollection<LogFileInfo> logFiles)
        {
            int deletedCount = 0;

            foreach (var logFile in logFiles)
            {
                if (File.Exists(logFile.FilePath))
                {
                    try
                    {
                        await Task.Run(() => File.Delete(logFile.FilePath));
                        logFile.UpdateExistenceStatus(); // Update status immediately
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file '{logFile.FileName}': {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            return deletedCount;
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

        public void CheckAllFilesExistence(ObservableCollection<LogFileInfo> logFiles)
        {
            foreach (var logFile in logFiles)
            {
                logFile.UpdateExistenceStatus();
            }
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

    public class LogFileInfo : INotifyPropertyChanged
    {
        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                UpdateExistenceStatus();
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string FileName { get; set; }
        public string FileSize { get; set; }
        public DateTime LastModified { get; set; }

        private Brush _statusIndicator;

        // Property that represents the visual status of the file
        public Brush StatusIndicator
        {
            get => _statusIndicator;
            set
            {
                _statusIndicator = value;
                OnPropertyChanged(nameof(StatusIndicator));
            }
        }

        [JsonIgnore] // Don't serialize the existence status as it will be recalculated on load
        public bool FileExists { get; private set; }

        // Method to update file existence status
        public void UpdateExistenceStatus()
        {
            FileExists = !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
            StatusIndicator = FileExists ? Brushes.Green : Brushes.Red;
            OnPropertyChanged(nameof(StatusIndicator));
            OnPropertyChanged(nameof(FileExists));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
