using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BS.CAD.Tools.Models;
using BS.CAD.Tools.Utils;

namespace BS.CAD.Tools.Services
{
    /// <summary>
    /// Reads and writes user-level settings for BS-CAD-Tools.
    /// Settings are stored outside the drawing, under %APPDATA%\BS-CAD-Tools.
    /// </summary>
    public class SettingsService
    {
        public const string LayerTools = "LayerTools";
        public const string ImeTools = "ImeTools";
        public const string FontTools = "FontTools";
        public const string CleanupTools = "CleanupTools";
        public const string StandardTools = "StandardTools";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public string AppDataDirectory { get; }
        public string SettingsFilePath { get; }

        public SettingsService()
        {
            AppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BS-CAD-Tools");
            SettingsFilePath = Path.Combine(AppDataDirectory, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                EnsureDirectory();

                if (!File.Exists(SettingsFilePath))
                {
                    var defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefaultSettings();
                Normalize(settings);
                return settings;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return CreateDefaultSettings();
            }
        }

        public bool Save(AppSettings settings)
        {
            try
            {
                EnsureDirectory();
                Normalize(settings);
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public bool IsModuleEnabled(string moduleKey)
        {
            var settings = Load();
            return settings.EnabledModules.TryGetValue(moduleKey, out bool enabled) && enabled;
        }

        public bool SetModuleEnabled(string moduleKey, bool enabled)
        {
            var settings = Load();
            settings.EnabledModules[moduleKey] = enabled;
            return Save(settings);
        }

        public static AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                EnabledModules = new Dictionary<string, bool>
                {
                    [LayerTools] = true,
                    [ImeTools] = true,
                    [FontTools] = true,
                    [CleanupTools] = false,
                    [StandardTools] = false
                }
            };
        }

        public static IReadOnlyList<string> KnownModuleKeys { get; } = new[]
        {
            LayerTools,
            ImeTools,
            FontTools,
            CleanupTools,
            StandardTools
        };

        private void EnsureDirectory()
        {
            if (!Directory.Exists(AppDataDirectory))
                Directory.CreateDirectory(AppDataDirectory);
        }

        private static void Normalize(AppSettings settings)
        {
            settings.EnabledModules ??= new Dictionary<string, bool>();

            var defaults = CreateDefaultSettings().EnabledModules;
            foreach (var pair in defaults)
            {
                if (!settings.EnabledModules.ContainsKey(pair.Key))
                    settings.EnabledModules[pair.Key] = pair.Value;
            }
        }
    }
}
