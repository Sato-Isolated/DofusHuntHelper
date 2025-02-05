using System.IO;
using System.Text.Json;

namespace DofusHuntHelper.Core;

public static class SettingsManager
{
    private static readonly string SettingsPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    public static AppSettings LoadSettings()
    {
        if (!File.Exists(SettingsPath))
            // Fichier inexistant => on retourne une config par défaut
            return new AppSettings();

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
            var json = JsonSerializer.Serialize(settings, JsonSerializerOptions);

            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Vous pouvez loguer, afficher un message, etc.
        }
    }
}