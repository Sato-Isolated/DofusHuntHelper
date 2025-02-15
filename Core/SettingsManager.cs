using System.IO;
using System.Text.Json;

namespace DofusHuntHelper.Core
{
    /// <summary>
    /// Gère le chargement et la sauvegarde des paramètres de l'application
    /// dans un fichier JSON (par défaut <c>settings.json</c>).
    /// </summary>
    public static class SettingsManager
    {
        /// <summary>
        /// Chemin complet du fichier de configuration
        /// (<c>settings.json</c> dans le répertoire de l'application).
        /// </summary>
        private static readonly string SettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        /// <summary>
        /// Options de sérialisation JSON (ici, indentées pour la lisibilité).
        /// </summary>
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Charge les paramètres de l'application depuis le fichier JSON.
        /// </summary>
        /// <returns>
        /// Une instance de <see cref="AppSettings"/> contenant
        /// les paramètres chargés ou, en cas d'erreur, une configuration par défaut.
        /// </returns>
        public static AppSettings LoadSettings()
        {
            // Si le fichier n'existe pas, on renvoie des paramètres par défaut
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                // Si la désérialisation échoue et renvoie null, on renvoie aussi des paramètres par défaut
                return settings ?? new AppSettings();
            }
            catch
            {
                // En cas d’erreur (fichier corrompu, problème d'accès, etc.),
                // on ne veut pas crasher l’appli : on renvoie simplement des paramètres par défaut
                return new AppSettings();
            }
        }

        /// <summary>
        /// Sauvegarde les paramètres de l'application dans le fichier JSON.
        /// </summary>
        /// <param name="settings">
        /// Paramètres à sauvegarder (instance de <see cref="AppSettings"/>).
        /// </param>
        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, JsonSerializerOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Ici, vous pouvez loguer l'exception, afficher un message à l'utilisateur, etc.
                // En l'état, on ignore silencieusement l'erreur de sauvegarde.
            }
        }
    }
}