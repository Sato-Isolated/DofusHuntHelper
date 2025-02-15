using System.Diagnostics;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;

namespace DofusHuntHelper.Core
{
    /// <summary>
    /// Surveille le presse-papier à intervalles réguliers afin de détecter
    /// tout changement dans le texte copié.
    /// </summary>
    public class ClipboardWatcher : IDisposable
    {
        private readonly int _intervalMs;
        private readonly DispatcherTimer _timer;
        private string _lastText = string.Empty;

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="ClipboardWatcher"/>
        /// qui surveille le presse-papier toutes les <paramref name="intervalMs"/> millisecondes.
        /// </summary>
        /// <param name="intervalMs">Intervalle (en millisecondes) entre deux contrôles du presse-papier.</param>
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
        /// Libère les ressources utilisées par le <see cref="ClipboardWatcher"/>.
        /// </summary>
        public void Dispose()
        {
            Debug.Print("[ClipboardWatcher] Dispose() appelé. Nettoyage des ressources...");
            Stop();
            _timer.Tick -= Timer_Tick;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Événement déclenché lorsque le texte du presse-papier change.
        /// </summary>
        public event Action<string>? OnClipboardTextChanged;

        /// <summary>
        /// Événement déclenché lorsqu’une erreur survient (exception en paramètre).
        /// </summary>
        public event Action<Exception>? OnError;

        /// <summary>
        /// Démarre la surveillance du presse-papier.
        /// </summary>
        public void Start()
        {
            if (_timer.IsEnabled)
            {
                Debug.Print("[ClipboardWatcher] Start() appelé alors que la surveillance est déjà active.");
                return;
            }

            Debug.Print("[ClipboardWatcher] Surveillance du presse-papier démarrée (toutes les {0} ms).",
                _intervalMs);
            _timer.Start();
        }

        /// <summary>
        /// Arrête la surveillance du presse-papier.
        /// </summary>
        public void Stop()
        {
            if (!_timer.IsEnabled)
            {
                Debug.Print("[ClipboardWatcher] Stop() appelé alors que la surveillance n'est pas active.");
                return;
            }

            Debug.Print("[ClipboardWatcher] Surveillance du presse-papier arrêtée.");
            _timer.Stop();
        }

        /// <summary>
        /// Méthode appelée à chaque tick du <see cref="DispatcherTimer"/>.
        /// Vérifie le texte actuel du presse-papier et déclenche
        /// <see cref="OnClipboardTextChanged"/> s’il a changé.
        /// </summary>
        /// <param name="sender">Objet émetteur de l’événement (inutilisé).</param>
        /// <param name="e">Informations sur l’événement.</param>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var currentText = Clipboard.GetText();
                Debug.Print("[ClipboardWatcher] Contenu actuel du presse-papier : {0}", currentText);

                // Déclenchement de l'événement seulement si le texte a changé
                if (currentText != _lastText)
                {
                    _lastText = currentText;
                    Debug.Print("[ClipboardWatcher] Nouveau texte détecté : {0}", currentText);

                    OnClipboardTextChanged?.Invoke(currentText);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("[ClipboardWatcher] Erreur lors de la lecture du presse-papier. {0}", ex);
                OnError?.Invoke(ex);
            }
        }
    }
}