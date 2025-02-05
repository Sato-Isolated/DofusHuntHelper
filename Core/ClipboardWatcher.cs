using System.Diagnostics;
using System.Windows.Threading;
using Serilog;
using Clipboard = System.Windows.Clipboard;

namespace DofusHuntHelper.Core;

public class ClipboardWatcher : IDisposable
{
    private readonly int _intervalMs;
    private readonly DispatcherTimer _timer;
    private string _lastText = string.Empty;

    /// <summary>
    ///     Initialise un ClipboardWatcher qui surveille le presse-papier toutes les <paramref name="intervalMs" />
    ///     millisecondes.
    /// </summary>
    /// <param name="intervalMs">Intervalle de vérification en millisecondes</param>
    public ClipboardWatcher(int intervalMs = 500)
    {
        _intervalMs = intervalMs;

        // Configuration du timer WPF
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(intervalMs)
        };
        _timer.Tick += Timer_Tick;

        Debug.Print("[ClipboardWatcher] Instance créée avec un intervalle de {0} ms.", intervalMs);
    }

    /// <summary>
    ///     Libère les ressources utilisées par le ClipboardWatcher.
    /// </summary>
    public void Dispose()
    {
        Debug.Print("[ClipboardWatcher] Dispose() appelé. Nettoyage des ressources...");
        Stop();
        _timer.Tick -= Timer_Tick;
        GC.SuppressFinalize(this);
    }

    public event Action<string>? OnClipboardTextChanged;
    public event Action<Exception>? OnError;

    /// <summary>
    ///     Démarre la surveillance du presse-papier.
    /// </summary>
    public void Start()
    {
        if (_timer.IsEnabled)
        {
            Debug.Print("[ClipboardWatcher] Start() appelé alors que la surveillance est déjà active.");
            return;
        }

        Log.Information("[ClipboardWatcher] Surveillance du presse-papier démarrée (toutes les {Interval} ms).",
            _intervalMs);
        _timer.Start();
    }

    /// <summary>
    ///     Arrête la surveillance du presse-papier.
    /// </summary>
    public void Stop()
    {
        if (!_timer.IsEnabled)
        {
            Debug.Print("[ClipboardWatcher] Stop() appelé alors que la surveillance n'est pas active.");
            return;
        }

        Log.Information("[ClipboardWatcher] Surveillance du presse-papier arrêtée.");
        _timer.Stop();
    }

    /// <summary>
    ///     Méthode appelée à chaque Tick du DispatcherTimer.
    /// </summary>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var currentText = Clipboard.GetText();
            Log.Verbose("[ClipboardWatcher] Contenu actuel du presse-papier : {Text}", currentText);

            // Déclenchement de l'événement seulement si le texte a changé
            if (currentText != _lastText)
            {
                _lastText = currentText;
                Log.Information("[ClipboardWatcher] Nouveau texte détecté : {Text}", currentText);

                OnClipboardTextChanged?.Invoke(currentText);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ClipboardWatcher] Erreur lors de la lecture du presse-papier.");
            OnError?.Invoke(ex);
        }
    }
}