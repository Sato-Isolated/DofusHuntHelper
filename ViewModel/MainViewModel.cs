using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Avalonia.Threading;
using DofusHuntHelper.Arduino;
using DofusHuntHelper.Commands;
using DofusHuntHelper.Core;
using Serilog;
using Cursor = System.Windows.Forms.Cursor;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using Screen = System.Windows.Forms.Screen;

namespace DofusHuntHelper.ViewModel;

public class MainViewModel : INotifyPropertyChanged
{
    #region Champs privés

    private bool _isRunning;
    private bool _useKeyboardSimulator;
    private string _port;
    private string _coordinatesDisplay;
    private string _outputLog;
    private int _selectedScreenIndex;
    private ObservableCollection<string> _screenNames;

    // Gestion du hook souris natif
    private IntPtr _mouseHookHandle = IntPtr.Zero;
    private readonly NativeMethods.HookProc _hookProc;
    private const int WhMouseLl = 14; // Identifiant du hook souris
    private const int WmLbuttondown = 0x0201;

    // Paramètres appli
    private readonly AppSettings _settings;

    // Arduino & ClipboardWatcher
    private ArduinoController _arduinoController;
    private ClipboardWatcher? _clipboardWatcher;
    private string _lastTravelText = string.Empty;

    #endregion

    #region Constructeur

    public MainViewModel()
    {
        Debug.Print("Initialisation du MainViewModel...");

        // Charger les settings
        _settings = SettingsManager.LoadSettings();
        Log.Information("Paramètres chargés : X={X}, Y={Y}, Port={Port}, Process={ProcessName}, Interval={Interval} ms",
            _settings.X, _settings.Y, _settings.Port, _settings.ProcessName, _settings.ClipboardCheckIntervalMs);

        // Affectation des champs initiaux
        _hookProc = MouseHookCallback;
        _port = _settings.Port;
        _coordinatesDisplay = $"Coordonnée: X={_settings.X}, Y={_settings.Y}";
        _outputLog = $"Paramètres chargés: X={_settings.X}, Y={_settings.Y}, " +
                     $"Port={_settings.Port}, Process={_settings.ProcessName}, " +
                     $"Interval={_settings.ClipboardCheckIntervalMs} ms\n";

        // Initialiser l’Arduino
        _arduinoController = new ArduinoController(_port);
        Debug.Print("ArduinoController créé pour le port {0}", _port);

        // Préparer la liste des écrans
        _screenNames = [];
        var screens = Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            _screenNames.Add($"DISPLAY{i + 1}");
            Debug.Print("Écran détecté : DISPLAY{0}", i + 1);
        }

        // Sélection par défaut
        if (_screenNames.Count > 0)
        {
            _selectedScreenIndex = 0;
            Debug.Print("Index d'écran par défaut fixé à 0");
        }

        // Créer les commandes (pour liaison avec le XAML)
        CaptureCoordinatesCommand = new RelayCommand(_ => CaptureCoordinates());
        StartStopCommand = new RelayCommand(_ => StartOrStop());

        Debug.Print("MainViewModel initialisé avec succès.");
    }

    #endregion

    #region Exécution de la commande /travel

    private async void PerformTravelCommandAsync(int savedX, int savedY)
    {
        Debug.Print("[MainViewModel] PerformTravelCommandAsync() exécuté avec X={0}, Y={1}", savedX, savedY);

        // Récupère la liste des écrans (WinForms) pour connaître les coordonnées
        var screens = Screen.AllScreens;
        if (SelectedScreenIndex < 0 || SelectedScreenIndex >= screens.Length)
        {
            Log.Warning("[MainViewModel] Écran sélectionné invalide. SelectedScreenIndex={Index}", SelectedScreenIndex);
            AppendLog("Écran sélectionné invalide.\n");
            return;
        }

        var screenBounds = screens[SelectedScreenIndex].Bounds;

        // Déplace le curseur : screenOffset + pos sauvegardée
        Cursor.Position = new Point(
            screenBounds.X + savedX,
            screenBounds.Y + savedY);

        Debug.Print("[MainViewModel] Curseur déplacé sur l'écran {0} à X={1}, Y={2}",
            SelectedScreenIndex, screenBounds.X + savedX, screenBounds.Y + savedY);

        // Vérifier que le processus ciblé existe
        var processes = Process.GetProcessesByName(_settings.ProcessName);
        if (processes.Length == 0)
        {
            Log.Warning("[MainViewModel] Processus '{ProcessName}' introuvable.", _settings.ProcessName);
            AppendLog($"Le processus '{_settings.ProcessName}' n'a pas été trouvé.\n");
            return;
        }

        var process = processes[0];
        var handle = process.MainWindowHandle;
        Debug.Print("[MainViewModel] Processus trouvé : {0}, ID={1}", process.ProcessName, process.Id);

        NativeMethods.SetForegroundWindow(handle);
        NativeMethods.mouse_event(
            NativeMethods.MouseeventfLeftdown | NativeMethods.MouseeventfLeftup,
            0, 0, 0, 0);

        NativeMethods.SetForegroundWindow(handle);

        // Envoi de la commande via simulateur clavier ou Arduino
        if (UseKeyboardSimulator)
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
                Log.Error(ex, "[MainViewModel] Erreur lors de la simulation clavier.");
                AppendLog($"Erreur clavier: {ex.Message}\n");
            }
        else
            try
            {
                Debug.Print("[MainViewModel] Envoi de la commande via Arduino");
                await _arduinoController.SendCommandAsync("1");
                await Task.Delay(200);
                await _arduinoController.SendCommandAsync("1");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MainViewModel] Erreur lors de l'envoi de la commande à l'Arduino.");
                AppendLog($"Erreur Arduino: {ex.Message}\n");
            }

        AppendLog("Action exécutée avec succès sur l'écran sélectionné !\n");
        Log.Information("[MainViewModel] Commande '/travel' exécutée avec succès.");
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        Debug.Print("Propriété modifiée : {0}", propName);
    }

    #endregion

    #region Propriétés bindables

    /// <summary>
    ///     Indique si on utilise le mode “clavier simulateur”
    ///     ou si on communique via Arduino.
    /// </summary>
    public bool UseKeyboardSimulator
    {
        get => _useKeyboardSimulator;
        set
        {
            if (_useKeyboardSimulator == value) return;
            Debug.Print("Changement de la propriété UseKeyboardSimulator -> {0}", value);
            _useKeyboardSimulator = value;
            OnPropertyChanged();

            if (_useKeyboardSimulator)
            {
                _arduinoController?.Disconnect();
                AppendLog("Mode clavier virtuel activé\n");
                Log.Information("Mode clavier virtuel activé. Arduino déconnecté.");
            }
            else
            {
                AppendLog("Mode Arduino activé\n");
                Log.Information("Mode Arduino activé.");
            }
        }
    }

    /// <summary>
    ///     Nom du port COM pour l’Arduino (COM3, COM4, etc.).
    /// </summary>
    public string Port
    {
        get => _port;
        set
        {
            if (_port == value) return;
            Debug.Print("Changement de la propriété Port -> {0}", value);
            _port = value;
            OnPropertyChanged();
            UpdateArduinoPort();
        }
    }

    /// <summary>
    ///     Affichage des coordonnées capturées (X=..., Y=...).
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
    ///     Log affiché dans l’UI.
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
    ///     Liste des noms d’écrans (“DISPLAY1”, “DISPLAY2”, etc.).
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
    ///     Écran actuellement sélectionné (index).
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
    ///     Commande pour capturer les coordonnées (hook souris).
    /// </summary>
    public ICommand CaptureCoordinatesCommand { get; }

    /// <summary>
    ///     Commande pour démarrer/stopper la surveillance (clipboard, etc.).
    /// </summary>
    public ICommand StartStopCommand { get; }

    /// <summary>
    ///     Texte dynamique du bouton Start/Stop (bind dans le XAML).
    /// </summary>
    public string StartStopButtonText => _isRunning ? "Stop" : "Start";

    #endregion

    #region Méthodes principales

    /// <summary>
    ///     Lance le hook souris pour capturer la position du prochain clic.
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
    ///     Callback du hook souris global.
    /// </summary>
    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            // Si c’est un clic gauche
            if (nCode >= 0 && wParam.ToInt32() == WmLbuttondown)
                if (NativeMethods.GetCursorPos(out var point))
                {
                    CoordinatesDisplay = $"Coordonnée: X={point.X}, Y={point.Y}";
                    AppendLog($"Coordonnée sauvegardée: X={point.X}, Y={point.Y}\n");
                    Log.Information("Coordonnées capturées : X={X}, Y={Y}", point.X, point.Y);

                    // Mise à jour des settings
                    _settings.X = point.X;
                    _settings.Y = point.Y;
                    SettingsManager.SaveSettings(_settings);
                    Debug.Print("Paramètres mis à jour et sauvegardés après capture de coordonnées.");

                    // On retire le hook
                    NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
                    _mouseHookHandle = IntPtr.Zero;

                    // MessageBox WPF
                    MessageBox.Show("Coordonnées sauvegardées avec succès !",
                        "Confirmation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du callback du hook souris");
        }

        return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    ///     Lance ou arrête la surveillance du presse-papier.
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
    ///     Démarre la surveillance (connexion Arduino si nécessaire, ClipboardWatcher, etc.)
    /// </summary>
    private void StartMonitoring()
    {
        Debug.Print("Début de StartMonitoring().");
        _isRunning = true;

        // Connexion Arduino si on n’est pas en mode simulateur
        if (!UseKeyboardSimulator)
        {
            Debug.Print("Tentative de connexion à l'Arduino (port : {0}).", _settings.Port);
            if (!_arduinoController.Connect())
            {
                Log.Error("Échec de la connexion à l'Arduino sur le port {Port}", _settings.Port);
                AppendLog("Échec de la connexion à l'Arduino.\n");
                _isRunning = false;
                return;
            }

            Log.Information("Arduino connecté avec succès sur {Port}.", _settings.Port);
        }

        // Lancer le ClipboardWatcher
        _clipboardWatcher = new ClipboardWatcher(_settings.ClipboardCheckIntervalMs);
        Debug.Print("ClipboardWatcher instancié avec un intervalle de {0} ms", _settings.ClipboardCheckIntervalMs);

        _clipboardWatcher.OnClipboardTextChanged += OnClipboardTextChanged;
        _clipboardWatcher.OnError += OnClipboardError;
        _clipboardWatcher.Start();

        Log.Information("Monitoring du presse-papier démarré (interval={Interval} ms).",
            _settings.ClipboardCheckIntervalMs);
        AppendLog($"Monitoring démarré (interval={_settings.ClipboardCheckIntervalMs} ms)\n");
    }

    /// <summary>
    ///     Arrête la surveillance (Arduino + ClipboardWatcher).
    /// </summary>
    private void StopMonitoring()
    {
        Debug.Print("Début de StopMonitoring().");
        _isRunning = false;

        if (_clipboardWatcher != null)
        {
            _clipboardWatcher.Stop();
            _clipboardWatcher = null;
            Debug.Print("ClipboardWatcher arrêté et mis à null.");
        }

        if (!UseKeyboardSimulator)
        {
            _arduinoController.Disconnect();
            Debug.Print("Arduino déconnecté.");
        }

        Log.Information("Monitoring du presse-papier stoppé.");
        AppendLog("Monitoring stoppé.\n");
    }

    #endregion

    #region Gestion du presse-papier

    /// <summary>
    ///     Méthode appelée lorsque le texte du presse-papier change.
    /// </summary>
    private void OnClipboardTextChanged(string newText)
    {
        Debug.Print("[MainViewModel] OnClipboardTextChanged invoqué avec : {0}", newText);

        // Vérifie si on est déjà sur le thread UI
        if (Dispatcher.UIThread.CheckAccess())
            ProcessClipboardText(newText);
        else
            // Post ou InvokeAsync selon vos besoins.
            Dispatcher.UIThread.Post(() => ProcessClipboardText(newText));
    }

    private void ProcessClipboardText(string newText)
    {
        Debug.Print("[MainViewModel] ProcessClipboardText() reçu : {0}", newText);

        // On vérifie si c’est une commande /travel et si elle est nouvelle
        if (newText != _lastTravelText && newText.Contains("/travel", StringComparison.OrdinalIgnoreCase))
        {
            _lastTravelText = newText;
            Log.Information("[MainViewModel] Commande '/travel' détectée ! Exécution...");

            PerformTravelCommandAsync(_settings.X, _settings.Y);
        }
        else
        {
            Log.Warning("[MainViewModel] Texte ignoré (déjà traité ou pas de '/travel') : {NewText}", newText);
        }
    }

    /// <summary>
    ///     Méthode appelée en cas d’erreur de lecture du presse-papier.
    /// </summary>
    private void OnClipboardError(Exception ex)
    {
        Log.Error(ex, "Erreur lors de la lecture du presse-papier (OnClipboardError).");

        if (Dispatcher.UIThread.CheckAccess())
            ShowClipboardError(ex);
        else
            Dispatcher.UIThread.Invoke(() => ShowClipboardError(ex));
    }

    private static void ShowClipboardError(Exception ex)
    {
        Log.Error(ex, "[GUI] Erreur du presse-papier");
        MessageBox.Show($"Erreur lors de la lecture du presse-papier : {ex.Message}",
            "Clipboard Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    #endregion

    #region Méthodes utilitaires & update du port

    private void ShowMonitorIndicator(int screenIndex)
    {
        AppendLog($"Écran sélectionné: {screenIndex + 1}\n");
        Debug.Print("Indicateur d'écran : index={0}. Overlay possible si nécessaire.", screenIndex + 1);
    }

    private void UpdateArduinoPort()
    {
        Debug.Print("Début de UpdateArduinoPort() avec le port {0}", _port);

        _arduinoController.Disconnect();
        Debug.Print("ArduinoController déconnecté avant mise à jour du port.");

        _settings.Port = _port;
        SettingsManager.SaveSettings(_settings);
        Debug.Print("Paramètres sauvegardés après mise à jour du port : {0}", _port);

        _arduinoController = new ArduinoController(_settings.Port);
        Log.Information("Port Arduino mis à jour sur {Port}.", _settings.Port);
    }

    private void AppendLog(string text)
    {
        Debug.Print("AppendLog appelé avec le texte : {0}", text.Replace("\n", "\\n"));
        OutputLog += text;
    }

    #endregion
}