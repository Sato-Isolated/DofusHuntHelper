using System;
using System.Windows.Forms;
using Serilog;

namespace DofusHuntHelper
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 1) Configuration de Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                // On écrit dans un fichier "app.log" avec rotation journalière :
                .WriteTo.File(
                    path: "app.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7, // garder 7 jours
                    rollOnFileSizeLimit: true)
                // Optionnel: on écrit aussi en console (utile en debug)
                .CreateLogger();

            // 2) Démarrage de l'application WinForms
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Log.Information("Application starting up...");
                Application.Run(new GUI());
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly.");
            }
            finally
            {
                // On flush et on ferme proprement le logger
                Log.CloseAndFlush();
            }
        }
    }
}