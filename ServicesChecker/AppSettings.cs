using Newtonsoft.Json;
using System;
using System.IO;

namespace ServicesChecker
{
    public class AppSettings
    {
        public string GitHubOwner { get; set; }
        public string GitHubRepo { get; set; }
        public bool CheckForUpdatesOnStartup { get; set; }
        
        private static AppSettings _instance;
        private const string SettingsFileName = "appsettings.json";
        
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        
        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    string json = File.ReadAllText(SettingsFileName);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? CreateDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return CreateDefault();
        }
        
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFileName, json);
                _instance = this;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        private static AppSettings CreateDefault()
        {
            var settings = new AppSettings
            {
                GitHubOwner = "YourGitHubUsername",
                GitHubRepo = "ServicesChecker",
                CheckForUpdatesOnStartup = true
            };
            
            // Save the default settings
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFileName, json);
            
            return settings;
        }
    }
}
