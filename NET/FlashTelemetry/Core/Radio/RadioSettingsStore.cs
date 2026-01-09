using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FlashTelemetry.Core.Radio
{
    /// <summary>
    /// Persistencia de ajustes del Team Radio por perfil.
    /// Se guarda en %AppData%\FlashTelemetry\radio_settings.json
    /// </summary>
    public class RadioSettingsStore
    {
        private readonly object _lock = new();
        private readonly string _path;
        private StoreRoot _root = new();

        public RadioSettingsStore(string? baseDir = null)
        {
            var dir = baseDir ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlashTelemetry");

            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "radio_settings.json");

            Load();
        }

        public RadioSettings GetForProfile(int profileId)
        {
            lock (_lock)
            {
                if (_root.ByProfile.TryGetValue(profileId.ToString(), out var s) && s is not null)
                    return s;

                var created = new RadioSettings();
                _root.ByProfile[profileId.ToString()] = created;
                Save();
                return created;
            }
        }

        public void SaveForProfile(int profileId, RadioSettings settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            lock (_lock)
            {
                _root.ByProfile[profileId.ToString()] = settings;
                Save();
            }
        }

        // Compatibilidad: algunos archivos antiguos llamaban a esto SetForProfile
        public void SetForProfile(int profileId, RadioSettings settings) => SaveForProfile(profileId, settings);

        public void Load()
        {
            lock (_lock)
            {
                if (!File.Exists(_path))
                {
                    _root = new StoreRoot();
                    return;
                }

                try
                {
                    var json = File.ReadAllText(_path);
                    _root = JsonSerializer.Deserialize<StoreRoot>(json, JsonOptions) ?? new StoreRoot();
                }
                catch
                {
                    _root = new StoreRoot();
                }
            }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_root, JsonOptions);
            File.WriteAllText(_path, json);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private sealed class StoreRoot
        {
            public Dictionary<string, RadioSettings> ByProfile { get; set; } = new();
        }
    }
}
