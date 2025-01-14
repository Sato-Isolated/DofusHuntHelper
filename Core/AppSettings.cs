namespace DofusHuntHelper.Core
{
    public class AppSettings
    {
        // Coordonnées pour le clic
        public int X { get; set; }
        public int Y { get; set; }

        // Port série
        public string Port { get; set; } = "COM3";

        // Nom du processus à cibler (par défaut "Dofus")
        public string ProcessName { get; set; } = "Dofus";

        // Intervalle de surveillance du presse-papiers (en ms)
        public int ClipboardCheckIntervalMs { get; set; } = 500;
    }
}