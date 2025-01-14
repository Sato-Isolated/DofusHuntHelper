using System;
using System.IO;
using System.Text.Json;

namespace DofusHuntHelper.Core
{
    public static class SettingsManager
    {
        private static readonly string SettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                // Fichier inexistant => on retourne une config par défaut
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                // En cas d’erreur, on ne veut pas crasher l’appli
                return new AppSettings();
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);

                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Vous pouvez loguer, afficher un message, etc.
            }
        }
    }
}