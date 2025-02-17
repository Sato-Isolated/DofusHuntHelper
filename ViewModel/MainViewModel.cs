using Avalonia.Threading;
using DofusHuntHelper.Arduino;
using DofusHuntHelper.Commands;
using DofusHuntHelper.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Cursor = System.Windows.Forms.Cursor;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using Screen = System.Windows.Forms.Screen;

namespace DofusHuntHelper.ViewModel
{
    /// <summary>
    /// Classe principale du ViewModel pour l'application. Gère :
    /// <list type="bullet">
    ///     <item>La connexion et la déconnexion à l'Arduino (ou l'utilisation d'un simulateur clavier).</item>
    ///     <item>La capture et l'enregistrement de coordonnées de clic via un hook souris.</item>
    ///     <item>La surveillance du presse-papier pour détecter les commandes spéciales (ex: "/travel").</item>
    ///     <item>L'interface INotifyPropertyChanged permettant la liaison (binding) avec la vue.</item>
    /// </list>
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Champs privés

        private bool _isRunning;
        // le KeyboardMode sera activer par défaut 
        // private bool _useKeyboardSimulator;
        // private string _port;
        private string _coordinatesDisplay;
        private string _outputLog;
        private int _selectedScreenIndex;
        private ObservableCollection<string> _screenNames;

        // Hook souris
        private IntPtr _mouseHookHandle = IntPtr.Zero;

        private readonly NativeMethods.HookProc _hookProc;
        private const int WhMouseLl = 14;
        private const int WmLbuttondown = 0x0201;

        // Paramètres et dépendances
        private readonly AppSettings _settings;

        // On désactive toutes les références à l'Arduino,  
        // car le mode clavier le remplace désormais et rend l'Arduino inutile.  
        // Toutefois, la classe est conservée au cas où un retour en arrière serait nécessaire.
        //private ArduinoController _arduinoController;
        private ClipboardWatcher? _clipboardWatcher;
        private string _lastTravelText = string.Empty;

        #endregion Champs privés

        #region Constructeur

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="MainViewModel"/>, charge les paramètres,
        /// configure l'ArduinoController, et prépare les listes d'écrans et les commandes.
        /// </summary>
        public MainViewModel()
        {
            Debug.Print("Initialisation du MainViewModel...");

            // Chargement des paramètres
            _settings = SettingsManager.LoadSettings();
            Debug.Print("Paramètres chargés : X={0}, Y={1}, Process={2}, Interval={3} ms",
                _settings.X, _settings.Y, _settings.ProcessName, _settings.ClipboardCheckIntervalMs);

            // Affectations initiales
            _hookProc = MouseHookCallback;
            _coordinatesDisplay = $"Coordonnée: X={_settings.X}, Y={_settings.Y}";
            _outputLog = $"Paramètres chargés: X={_settings.X}, Y={_settings.Y}, " +
                         $"Process={_settings.ProcessName}, " +
                         $"Interval={_settings.ClipboardCheckIntervalMs} ms\n";

            // Initialisation de l’Arduino
            // _arduinoController = new ArduinoController(_port);
            //Debug.Print("ArduinoController créé pour le port {0}", _port);


            // Préparation de la liste des écrans détectés
            _screenNames = new ObservableCollection<string>();
            var screens = Screen.AllScreens;
            for (var i = 0; i < screens.Length; i++)
            {
                _screenNames.Add($"DISPLAY{i + 1}");
                Debug.Print("Écran détecté : DISPLAY{0}", i + 1);
            }

            // Sélection de l’écran par défaut (le premier s’il existe)
            if (_screenNames.Count > 0)
            {
                _selectedScreenIndex = 0;
                Debug.Print("Index d'écran par défaut fixé à 0");
            }

            // Commandes associées aux actions du ViewModel
            CaptureCoordinatesCommand = new RelayCommand(_ => CaptureCoordinates());
            StartStopCommand = new RelayCommand(_ => StartOrStop());

            Debug.Print("MainViewModel initialisé avec succès.");
        }

        #endregion Constructeur

        #region Gestion de la commande "/travel"

        /// <summary>
        /// Exécute la commande '/travel' en :
        /// <list type="bullet">
        ///     <item>Déplaçant le curseur de la souris à la position capturée.</item>
        ///     <item>Positionnant la fenêtre du jeu au premier plan.</item>
        ///     <item>Simulant l'envoi de la commande, soit via le simulateur de clavier, soit via l’Arduino.</item>
        /// </list>
        /// </summary>
        /// <param name="savedX">Coordonnée X sauvegardée (click).</param>
        /// <param name="savedY">Coordonnée Y sauvegardée (click).</param>
        private async void PerformTravelCommandAsync(int savedX, int savedY)
        {
            Debug.Print("[MainViewModel] PerformTravelCommandAsync() exécuté avec X={0}, Y={1}", savedX, savedY);

            // Récupère la liste des écrans pour obtenir leurs dimensions
            var screens = Screen.AllScreens;
            if (SelectedScreenIndex < 0 || SelectedScreenIndex >= screens.Length)
            {
                Debug.Print("[MainViewModel] Écran sélectionné invalide. SelectedScreenIndex={0}", SelectedScreenIndex);
                AppendLog("Écran sélectionné invalide.\n");
                return;
            }

            // Calcule la position réelle du curseur (décalée de la position de l'écran)
            var screenBounds = screens[SelectedScreenIndex].Bounds;
            Cursor.Position = new Point(
                screenBounds.X + savedX,
                screenBounds.Y + savedY);

            Debug.Print("[MainViewModel] Curseur déplacé sur l'écran {0} à X={1}, Y={2}",
                SelectedScreenIndex, screenBounds.X + savedX, screenBounds.Y + savedY);

            // Vérifie l'existence du processus cible
            var processes = Process.GetProcessesByName(_settings.ProcessName);
            if (processes.Length == 0)
            {
                Debug.Print("[MainViewModel] Processus '{0}' introuvable.", _settings.ProcessName);
                AppendLog($"Le processus '{_settings.ProcessName}' n'a pas été trouvé.\n");
                return;
            }

            var process = processes[0];
            var handle = process.MainWindowHandle;
            Debug.Print("[MainViewModel] Processus trouvé : {0}, ID={1}", process.ProcessName, process.Id);

            // Met la fenêtre du jeu au premier plan et simule un clic
            NativeMethods.SetForegroundWindow(handle);
            NativeMethods.mouse_event(
                NativeMethods.MouseeventfLeftdown | NativeMethods.MouseeventfLeftup,
                0, 0, 0, 0);

            NativeMethods.SetForegroundWindow(handle);

            // Envoie la commande "/travel" soit via le simulateur, soit via l'Arduino
            //if (UseKeyboardSimulator)
            //{
                try
                {
                    Debug.Print("[MainViewModel] Envoi de la commande via KeyboardSimulator (Ctrl+V + ENTER)");
                    KeyboardSimulator.SendCombination(
                        KeyboardSimulator.VirtualKey.VkControl,
                        KeyboardSimulator.VirtualKey.VkV);
                    await Task.Delay(50);
                    KeyboardSimulator.SendRealisticEnter();
                    await Task.Delay(200);
                    KeyboardSimulator.SendRealisticEnter();
                }
                catch (Exception ex)
                {
                    AppendLog($"Erreur clavier: {ex.Message}\n");
                }
            //}
            //else
            //{
            //    try
            //    {
            //        Debug.Print("[MainViewModel] Envoi de la commande via Arduino");
            //        await _arduinoController.SendCommandAsync("1");
            //        await Task.Delay(200);
            //        await _arduinoController.SendCommandAsync("1");
            //    }
            //    catch (Exception ex)
            //    {
            //        AppendLog($"Erreur Arduino: {ex.Message}\n");
            //    }
            //}

            AppendLog("Action exécutée avec succès sur l'écran sélectionné !\n");
        }

        #endregion Gestion de la commande "/travel"

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifie l'interface d'un changement de propriété (liaison dynamique).
        /// </summary>
        /// <param name="propName">
        /// Nom de la propriété modifiée. Renseigné automatiquement
        /// via l'attribut <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        private void OnPropertyChanged([CallerMemberName] string propName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            Debug.Print("Propriété modifiée : {0}", propName);
        }

        #endregion INotifyPropertyChanged

        #region Propriétés bindables

        /// <summary>
        /// Indique si on utilise un simulateur de clavier (true) ou l’Arduino (false).
        /// </summary>
        //public bool UseKeyboardSimulator
        //{
        //    get => _useKeyboardSimulator;
        //    set
        //    {
        //        if (_useKeyboardSimulator == value) return;
        //        Debug.Print("Changement de la propriété UseKeyboardSimulator -> {0}", value);
        //        _useKeyboardSimulator = value;
        //        OnPropertyChanged();

        //        if (_useKeyboardSimulator)
        //        {
        //            // Lorsqu'on passe en mode simulateur, on déconnecte l'Arduino
        //            _arduinoController?.Disconnect();
        //            AppendLog("Mode clavier virtuel activé\n");
        //            Debug.Print("Mode clavier virtuel activé. Arduino déconnecté.");
        //        }
        //        else
        //        {
        //            AppendLog("Mode Arduino activé\n");
        //            Debug.Print("Mode Arduino activé.");
        //        }
        //    }
        //}

        /// <summary>
        /// Nom du port série de l’Arduino (par ex: "COM3").
        /// </summary>
        //public string Port
        //{
        //    get => _port;
        //    set
        //    {
        //        if (_port == value) return;
        //        Debug.Print("Changement de la propriété Port -> {0}", value);
        //        _port = value;
        //        OnPropertyChanged();
        //        UpdateArduinoPort();
        //    }
        //}

        /// <summary>
        /// Chaîne affichant les coordonnées capturées (X, Y).
        /// </summary>
        public string CoordinatesDisplay
        {
            get => _coordinatesDisplay;
            set
            {
                if (_coordinatesDisplay == value) return;
                Debug.Print("Changement de la propriété CoordinatesDisplay -> {0}", value);
                _coordinatesDisplay = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Texte de log à afficher dans l’interface utilisateur.
        /// </summary>
        public string OutputLog
        {
            get => _outputLog;
            set
            {
                if (_outputLog == value) return;
                _outputLog = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Liste des noms d'écrans détectés (ex. "DISPLAY1", "DISPLAY2").
        /// </summary>
        public ObservableCollection<string> ScreenNames
        {
            get => _screenNames;
            set
            {
                if (_screenNames == value) return;
                _screenNames = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Index de l'écran actuellement sélectionné dans la liste <see cref="ScreenNames"/>.
        /// </summary>
        public int SelectedScreenIndex
        {
            get => _selectedScreenIndex;
            set
            {
                if (_selectedScreenIndex == value) return;
                Debug.Print("Changement de la propriété SelectedScreenIndex -> {0}", value);
                _selectedScreenIndex = value;
                OnPropertyChanged();
                ShowMonitorIndicator(value);
            }
        }

        /// <summary>
        /// Commande pour capturer les coordonnées du prochain clic gauche.
        /// </summary>
        public ICommand CaptureCoordinatesCommand { get; }

        /// <summary>
        /// Commande pour démarrer/arrêter la surveillance (presse-papier, Arduino, etc.).
        /// </summary>
        public ICommand StartStopCommand { get; }

        /// <summary>
        /// Texte affiché sur le bouton Start/Stop dans la vue (dépend de l’état de la surveillance).
        /// </summary>
        public string StartStopButtonText => _isRunning ? "Stop" : "Start";

        #endregion Propriétés bindables

        #region Méthodes principales

        /// <summary>
        /// Lance un hook souris de bas niveau pour capturer la position du prochain clic gauche.
        /// </summary>
        private void CaptureCoordinates()
        {
            Debug.Print("Début de CaptureCoordinates() : activation du hook souris.");

            _mouseHookHandle = NativeMethods.SetWindowsHookEx(
                WhMouseLl,
                _hookProc,
                IntPtr.Zero,
                0);

            CoordinatesDisplay = "Cliquez avec le bouton gauche pour capturer les coordonnées...";
            AppendLog("Veuillez cliquer sur l'écran pour capturer les coordonnées...\n");
        }

        /// <summary>
        /// Callback du hook souris. Capture la position lors du clic gauche,
        /// sauvegarde les nouvelles coordonnées dans les settings, puis désinstalle le hook.
        /// </summary>
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && wParam.ToInt32() == WmLbuttondown)
                {
                    if (NativeMethods.GetCursorPos(out var point))
                    {
                        CoordinatesDisplay = $"Coordonnée: X={point.X}, Y={point.Y}";
                        AppendLog($"Coordonnée sauvegardée: X={point.X}, Y={point.Y}\n");
                        Debug.Print("Coordonnées capturées : X={0}, Y={1}", point.X, point.Y);

                        // Mise à jour des paramètres
                        _settings.X = point.X;
                        _settings.Y = point.Y;
                        SettingsManager.SaveSettings(_settings);
                        Debug.Print("Paramètres mis à jour et sauvegardés après capture de coordonnées.");

                        // Retrait du hook
                        NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
                        _mouseHookHandle = IntPtr.Zero;

                        // Avertissement à l'utilisateur
                        MessageBox.Show("Coordonnées sauvegardées avec succès !",
                            "Confirmation",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch
            {
                AppendLog("Erreur lors du callback du hook souris");
            }

            return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// Lance ou arrête la surveillance du presse-papier (et éventuellement de l'Arduino).
        /// </summary>
        private void StartOrStop()
        {
            Debug.Print("Début de StartOrStop() : état actuel _isRunning={0}", _isRunning);
            if (!_isRunning)
                StartMonitoring();
            else
                StopMonitoring();

            // Notifie l’UI que le texte du bouton a changé
            OnPropertyChanged(nameof(StartStopButtonText));
        }

        /// <summary>
        /// Démarre la surveillance : connexion Arduino si nécessaire, et initialisation du ClipboardWatcher.
        /// </summary>
        private void StartMonitoring()
        {
            Debug.Print("Début de StartMonitoring().");
            _isRunning = true;

            // Connexion à l’Arduino si on est en mode Arduino
            //if (!UseKeyboardSimulator)
            //{
            //    Debug.Print("Tentative de connexion à l'Arduino (port : {0}).", _settings.Port);
            //    if (!_arduinoController.Connect())
            //    {
            //        AppendLog("Échec de la connexion à l'Arduino.\n");
            //        _isRunning = false;
            //        return;
            //    }

            //    Debug.Print("Arduino connecté avec succès sur {0}.", _settings.Port);
            //}

            // Configuration du ClipboardWatcher
            _clipboardWatcher = new ClipboardWatcher(_settings.ClipboardCheckIntervalMs);
            Debug.Print("ClipboardWatcher instancié avec un intervalle de {0} ms", _settings.ClipboardCheckIntervalMs);

            _clipboardWatcher.OnClipboardTextChanged += OnClipboardTextChanged;
            _clipboardWatcher.OnError += OnClipboardError;
            _clipboardWatcher.Start();

            Debug.Print("Monitoring du presse-papier démarré (interval={0} ms).",
                _settings.ClipboardCheckIntervalMs);
            AppendLog($"Monitoring démarré (interval={_settings.ClipboardCheckIntervalMs} ms)\n");
        }

        /// <summary>
        /// Arrête la surveillance : déconnecte l’Arduino et arrête le ClipboardWatcher.
        /// </summary>
        private void StopMonitoring()
        {
            Debug.Print("Début de StopMonitoring().");
            _isRunning = false;

            //if (!UseKeyboardSimulator)
            //{
            //    _arduinoController.Disconnect();
            //    Debug.Print("Arduino déconnecté.");
            //}

            if (_clipboardWatcher != null)
            {
                _clipboardWatcher.Stop();
                _clipboardWatcher = null;
                Debug.Print("ClipboardWatcher arrêté et mis à null.");
            }

            Debug.Print("Monitoring du presse-papier stoppé.");
            AppendLog("Monitoring stoppé.\n");
        }

        #endregion Méthodes principales

        #region Gestion du presse-papier

        /// <summary>
        /// Appelé lorsque le texte du presse-papier change.
        /// Vérifie si on est sur le thread UI, puis exécute <see cref="ProcessClipboardText"/>.
        /// </summary>
        private void OnClipboardTextChanged(string newText)
        {
            Debug.Print("[MainViewModel] OnClipboardTextChanged invoqué avec : {0}", newText);

            // Vérifier si on est sur le thread UI
            if (Dispatcher.UIThread.CheckAccess())
            {
                ProcessClipboardText(newText);
            }
            else
            {
                Dispatcher.UIThread.Post(() => ProcessClipboardText(newText));
            }
        }

        /// <summary>
        /// Analyse le texte du presse-papier. Si c'est une commande "/travel",
        /// appelle <see cref="PerformTravelCommandAsync"/>.
        /// </summary>
        private void ProcessClipboardText(string newText)
        {
            Debug.Print("[MainViewModel] ProcessClipboardText() reçu : {0}", newText);

            // Vérifie s’il s’agit d’une nouvelle commande "/travel"
            if (newText != _lastTravelText && newText.Contains("/travel", StringComparison.OrdinalIgnoreCase))
            {
                _lastTravelText = newText;
                Debug.Print("[MainViewModel] Commande '/travel' détectée ! Exécution...");

                PerformTravelCommandAsync(_settings.X, _settings.Y);
            }
            else
            {
                // Ignore tout autre texte ou répétition
                Debug.Print("[MainViewModel] Texte ignoré (déjà traité ou pas de '/travel') : {0}", newText);
            }
        }

        /// <summary>
        /// Appelé lorsque le ClipboardWatcher rencontre une exception.
        /// Affiche un message d’erreur dans la console et un message à l'utilisateur.
        /// </summary>
        private void OnClipboardError(Exception ex)
        {
            if (Dispatcher.UIThread.CheckAccess())
                ShowClipboardError(ex);
            else
                Dispatcher.UIThread.Invoke(() => ShowClipboardError(ex));
        }

        /// <summary>
        /// Affiche un message d’erreur relatif au presse-papier dans une boîte de dialogue.
        /// </summary>
        private static void ShowClipboardError(Exception ex)
        {
            MessageBox.Show($"Erreur lors de la lecture du presse-papier : {ex.Message}",
                "Clipboard Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion Gestion du presse-papier

        #region Méthodes utilitaires

        /// <summary>
        /// Affiche un indicateur ou un log lorsqu'un écran est sélectionné.
        /// </summary>
        private void ShowMonitorIndicator(int screenIndex)
        {
            AppendLog($"Écran sélectionné: {screenIndex + 1}\n");
            Debug.Print("Indicateur d'écran : index={0}. Overlay possible si nécessaire.", screenIndex + 1);
        }

        /// <summary>
        /// Met à jour le port de l'Arduino, en déconnectant l'ancien contrôleur
        /// puis en créant un nouveau sur le port spécifié.
        /// </summary>
        //private void UpdateArduinoPort()
        //{
        //    Debug.Print("Début de UpdateArduinoPort() avec le port {0}", _port);

        //    _arduinoController.Disconnect();
        //    Debug.Print("ArduinoController déconnecté avant mise à jour du port.");

        //    _settings.Port = _port;
        //    SettingsManager.SaveSettings(_settings);
        //    Debug.Print("Paramètres sauvegardés après mise à jour du port : {0}", _port);

        //    _arduinoController = new ArduinoController(_settings.Port);
        //    Debug.Print("Port Arduino mis à jour sur {0}.", _settings.Port);
        //}

        /// <summary>
        /// Ajoute du texte au log de l’interface et l’affiche.
        /// </summary>
        private void AppendLog(string text)
        {
            Debug.Print("AppendLog appelé avec le texte : {0}", text.Replace("\n", "\\n"));
            OutputLog += text;
        }

        #endregion Méthodes utilitaires
    }
}