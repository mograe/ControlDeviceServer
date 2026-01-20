using ControlDeviceServer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ControlDeviceServer.Services
{
    public static class ConfigStore
    {
        static readonly JsonSerializerOptions JsonOpt = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ControlDeviceServer",
                "config.json"
            );

        public static ConfigSerialized LoadOrDefault()
        {
            try
            {
                var path = ConfigPath;
                if (!File.Exists(path))
                    return new ConfigSerialized();

                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<ConfigSerialized>(json, JsonOpt);
                return cfg ?? new ConfigSerialized();
            }
            catch
            {
                return new ConfigSerialized();
            }
        }

        public static void Save(ConfigSerialized cfg)
        {
            try
            {
                var path = ConfigPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var json = JsonSerializer.Serialize(cfg, JsonOpt);
                File.WriteAllText(path, json);
            }

            catch (Exception ex) 
            {
                MessageBox.Show($"Error Save: {ex.Message}", "Error Save", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
