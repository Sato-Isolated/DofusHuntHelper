namespace DofusHuntHelper.Core
{
    /// <summary>
    /// Représente la configuration de l'application, notamment les coordonnées de clic,
    /// le port série, le nom du processus à surveiller et l'intervalle de surveillance du presse-papiers.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Coordonnée X pour le clic (par exemple, pour un clic automatisé).
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Coordonnée Y pour le clic (par exemple, pour un clic automatisé).
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Nom du port série à utiliser (par défaut <c>"COM3"</c>).
        /// </summary>
       // public string Port { get; set; } = "COM3";

        /// <summary>
        /// Nom du processus à cibler (par défaut <c>"Dofus"</c>).
        /// Peut être utilisé pour vérifier si le jeu est en cours d’exécution.
        /// </summary>
        public string ProcessName { get; set; } = "Dofus";

        /// <summary>
        /// Intervalle de surveillance du presse-papiers, en millisecondes (par défaut 500 ms).
        /// </summary>
        public int ClipboardCheckIntervalMs { get; set; } = 500;
    }
}