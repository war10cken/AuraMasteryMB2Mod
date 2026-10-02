using System;
using System.IO;
using System.Xml.Serialization;
using AuraMastery.Core;

namespace AuraMastery.Config
{
    public static class AuraConfigLoader
    {
        private static AuraConfig _cachedConfig;
        private static readonly object _lock = new object();
        private static string _lastConfigPath;
        
        public static AuraConfig GetConfig()
        {
            if (_cachedConfig == null)
            {
                lock (_lock)
                {
                    if (_cachedConfig == null)
                    {
                        _cachedConfig = LoadOrCreateConfig();
                    }
                }
            }
            return _cachedConfig;
        }
        
        public static void ForceCreateConfig()
        {
            lock (_lock)
            {
                _cachedConfig = null;
                _cachedConfig = LoadOrCreateConfig();
            }
        }
        
        public static void ReloadConfig()
        {
            lock (_lock)
            {
                _cachedConfig = LoadOrCreateConfig();
            }
        }
        
        public static string GetConfigPath()
        {
            if (!string.IsNullOrEmpty(_lastConfigPath))
                return _lastConfigPath;
                
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _lastConfigPath = Path.Combine(
                documentsPath,
                "Mount and Blade II Bannerlord",
                "Configs",
                "AuraMastery",
                "AuraConfig.xml"
            );
            return _lastConfigPath;
        }
        
        private static AuraConfig LoadOrCreateConfig()
        {
            string configPath = GetConfigPath();
            AuraLogger.Log($"Config path: {configPath}");
            
            if (!File.Exists(configPath))
            {
                AuraLogger.Log("Config file not found, creating default...");
                CreateDefaultConfig(configPath);
            }
            else
            {
                AuraLogger.Log("Config file found, loading...");
            }
            
            try
            {
                var serializer = new XmlSerializer(typeof(AuraConfig));
                using (var reader = new StreamReader(configPath))
                {
                    var config = (AuraConfig)serializer.Deserialize(reader);
                    AuraLogger.Log($"Config loaded successfully: ActivateKey={config?.ActivateKey}, OpenMenuKey={config?.OpenMenuKey}");
                    return config ?? new AuraConfig();
                }
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"Failed to load config: {ex.Message}");
                AuraLogger.Log($"Creating default config due to load failure...");
                try
                {
                    File.Delete(configPath);
                }
                catch { }
                CreateDefaultConfig(configPath);
                return new AuraConfig();
            }
        }
        
        private static void CreateDefaultConfig(string configPath)
        {
            try
            {
                string directory = Path.GetDirectoryName(configPath);
                AuraLogger.Log($"Creating directory: {directory}");
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AuraLogger.Log("Directory created");
                }
                
                var defaultConfig = new AuraConfig();
                
                var serializer = new XmlSerializer(typeof(AuraConfig));
                using (var writer = new StreamWriter(configPath))
                {
                    serializer.Serialize(writer, defaultConfig);
                }
                
                AuraLogger.Log($"Default config created at: {configPath}");
            }
            catch (Exception ex)
            {
                AuraLogger.Log($"Failed to create default config: {ex.Message}");
                AuraLogger.Log($"Full exception: {ex}");
            }
        }
    }
}