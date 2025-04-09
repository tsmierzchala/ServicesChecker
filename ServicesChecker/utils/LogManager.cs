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

                // Update file existence status and size for all loaded files
                foreach (var logFile in logFiles)
                {
                    logFile.UpdateExistenceAndSizeStatus();
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
                    FileSize = FormatFileSize(fileInfo.Length)
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
                        logFile.UpdateExistenceAndSizeStatus(); // Update status immediately
                        deletedCount++;
                    }
                    catch (IOException ex)
                    {
                        // Obs³uga pliku zajêtego przez inny proces
                        MessageBox.Show($"Plik '{logFile.FileName}' jest u¿ywany przez inny proces i nie mo¿e zostaæ usuniêty.",
                            "B³¹d", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // Obs³uga braku uprawnieñ
                        MessageBox.Show($"Brak uprawnieñ do usuniêcia pliku '{logFile.FileName}'.",
                            "B³¹d", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (Exception ex)
                    {
                        // Obs³uga innych wyj¹tków
                        MessageBox.Show($"Nie mo¿na usun¹æ pliku '{logFile.FileName}': {ex.Message}",
                            "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
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
                logFile.UpdateExistenceAndSizeStatus();
            }
        }

        public void UpdateFileSizes(ObservableCollection<LogFileInfo> logFiles)
        {
            foreach (var logFile in logFiles)
            {
                if (logFile.FileExists && File.Exists(logFile.FilePath))
                {
                    var fileInfo = new FileInfo(logFile.FilePath);
                    logFile.FileSize = FormatFileSize(fileInfo.Length);
                }
            }
        }

        public string FormatFileSize(long bytes)
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
                UpdateExistenceAndSizeStatus();
                OnPropertyChanged(nameof(FilePath));
            }
        }

        private string _fileName;
        public string FileName 
        { 
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        private string _fileSize;
        public string FileSize 
        { 
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
            }
        }

        private Brush _statusIndicator;
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

        // Method to update file existence status and size
        public void UpdateExistenceAndSizeStatus()
        {
            FileExists = !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
            StatusIndicator = FileExists ? Brushes.Green : Brushes.Red;
            
            // Update file size if file exists
            if (FileExists)
            {
                var fileInfo = new FileInfo(FilePath);
                if (fileInfo.Exists)
                {
                    FileSize = new LogManager().FormatFileSize(fileInfo.Length);
                }
            }
            
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
