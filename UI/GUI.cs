using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DofusHuntHelper.Core;
using DofusHuntHelper.Arduino;
using Serilog;
using System.IO;

namespace DofusHuntHelper
{
    public partial class GUI : Form
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        // Mouse hook
        private readonly NativeMethods.HookProc _hookProc;
        private IntPtr _mouseHookHandle = IntPtr.Zero;

        // Arduino
        private ArduinoController _arduinoController;

        // Clipboard watcher
        private ClipboardWatcher? _clipboardWatcher;
        private bool _isRunning;
        private string _lastTravelText = string.Empty;

        // Les paramètres
        private AppSettings _settings;

        public GUI()
        {
            InitializeComponent();

            // Charger les settings
            _settings = SettingsManager.LoadSettings();

            // Delegate du mouse hook
            _hookProc = MouseHookCallback;

            // Init l'UI
            textBoxPort.Text = _settings.Port;
            lblCoordinates.Text = $"Coordonnée: X={_settings.X}, Y={_settings.Y}";
            richTextBox1.AppendText(
                $"Paramètres chargés: X={_settings.X}, Y={_settings.Y}, Port={_settings.Port}, " +
                $"Process={_settings.ProcessName}, Interval={_settings.ClipboardCheckIntervalMs} ms\n");

            // Instancie le contrôleur Arduino
            _arduinoController = new ArduinoController(_settings.Port);

            // Mise à jour du port quand la textbox change
            textBoxPort.TextChanged += (s, e) => UpdateArduinoPort();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            var monitorNames = Screen.AllScreens
                .Select((s, index) => $"DISPLAY{index + 1}")
                .ToArray();

            comboBoxScreens.Items.AddRange(monitorNames);
            if (comboBoxScreens.Items.Count > 0)
                comboBoxScreens.SelectedIndex = 0;
        }

        #region Mouse Hook

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            // Installe le hook souris
            _mouseHookHandle = NativeMethods.SetWindowsHookEx(
                WH_MOUSE_LL,
                _hookProc,
                IntPtr.Zero,
                0);

            lblCoordinates.Text = "Cliquez avec le bouton gauche pour capturer les coordonnées...";
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                if (NativeMethods.GetCursorPos(out var point))
                {
                    lblCoordinates.Text = $"Coordonnée: X={point.X}, Y={point.Y}";
                    richTextBox1.AppendText($"Coordonnée sauvegardée: X={point.X}, Y={point.Y}\n");

                    // Mise à jour dans settings
                    _settings.X = point.X;
                    _settings.Y = point.Y;
                    SettingsManager.SaveSettings(_settings);

                    // Retire le hook
                    NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
                    _mouseHookHandle = IntPtr.Zero;

                    MessageBox.Show("Coordonnées sauvegardées avec succès !",
                                    "Confirmation",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        #endregion

        #region Clipboard Monitoring

        private void buttonStartStop_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }

        private void StartMonitoring()
        {
            _isRunning = true;
            buttonStartStop.Text = "Stop";

            if (!_arduinoController.Connect())
            {
                Log.Error("Failed to connect Arduino on port {Port}", _settings.Port);
                MessageBox.Show("Échec de la connexion à l'Arduino.", "Erreur",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                _isRunning = false;
                buttonStartStop.Text = "Start";
                return;
            }

            // Créer le ClipboardWatcher
            _clipboardWatcher = new ClipboardWatcher(_settings.ClipboardCheckIntervalMs);
            _clipboardWatcher.OnClipboardTextChanged += OnClipboardTextChanged;
            _clipboardWatcher.OnError += OnClipboardError;
            _clipboardWatcher.Start();

            Log.Information("Clipboard monitoring started (interval={Interval} ms).", _settings.ClipboardCheckIntervalMs);
        }

        private void OnClipboardTextChanged(string newText)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnClipboardTextChanged(newText)));
                return;
            }

            // On ne traite l'action que si c'est un texte avec "/travel"
            if (newText != _lastTravelText && newText.Contains("/travel"))
            {
                _lastTravelText = newText;
                PerformTravelCommandAsync(_settings.X, _settings.Y);
            }
        }

        private void OnClipboardError(Exception ex)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnClipboardError(ex)));
                return;
            }

            Log.Error(ex, "[GUI] Clipboard error");
            MessageBox.Show("Erreur Clipboard: " + ex.Message,
                            "Erreur",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
        }

        private void StopMonitoring()
        {
            _isRunning = false;
            buttonStartStop.Text = "Start";

            _clipboardWatcher?.Stop();
            _clipboardWatcher = null;

            _arduinoController.Disconnect();

            Log.Information("Clipboard monitoring stopped.");
        }

        #endregion

        #region Travel Command

        private async void PerformTravelCommandAsync(int savedX, int savedY)
        {
            // Sélection de l'écran
            var selectedIndex = comboBoxScreens.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= Screen.AllScreens.Length)
            {
                richTextBox1.AppendText("Écran sélectionné invalide.\n");
                return;
            }

            var screenBounds = Screen.AllScreens[selectedIndex].Bounds;
            Cursor.Position = new Point(screenBounds.X + savedX, screenBounds.Y + savedY);

            // On récupère le(s) process 
            var processes = Process.GetProcessesByName(_settings.ProcessName);
            if (processes.Length == 0)
            {
                richTextBox1.AppendText($"Le processus '{_settings.ProcessName}' n'a pas été trouvé.\n");
                Log.Warning("Process {ProcessName} not found.", _settings.ProcessName);
                return;
            }
            else if (processes.Length > 1)
            {
                richTextBox1.AppendText($"Plusieurs processus '{_settings.ProcessName}' trouvés. On utilise le premier.\n");
                Log.Warning("Multiple processes named {ProcessName}. Using the first.", _settings.ProcessName);
            }

            var process = processes[0];
            var handle = process.MainWindowHandle;

            // Met la fenêtre au premier plan
            NativeMethods.SetForegroundWindow(handle);

            // Clic gauche
            NativeMethods.mouse_event(
                NativeMethods.MOUSEEVENTF_LEFTDOWN | NativeMethods.MOUSEEVENTF_LEFTUP,
                0, 0, 0, 0);

            // Colle le contenu (Ctrl + V)
            NativeMethods.SetForegroundWindow(handle);
            SendKeys.SendWait("^v");

            // Re-focalise le process
            NativeMethods.SetForegroundWindow(handle);

            // Envoie 2x la commande "1" à l’Arduino
            await _arduinoController.SendCommandAsync("1");
            await Task.Delay(200);
            await _arduinoController.SendCommandAsync("1");

            richTextBox1.AppendText("Action exécutée avec succès sur l'écran sélectionné !\n");
            Log.Information("Travel command executed successfully.");
        }

        #endregion

        #region Screen Indicator

        private void comboBoxScreens_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndex = comboBoxScreens.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < Screen.AllScreens.Length)
            {
                ShowMonitorIndicator(selectedIndex, selectedIndex + 1);
            }
        }

        private void ShowMonitorIndicator(int screenIndex, int displayNumber)
        {
            var screen = Screen.AllScreens[screenIndex];

            var indicatorForm = new Form
            {
                BackColor = Color.Black,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(screen.Bounds.Right - 200, screen.Bounds.Top + 50, 150, 150),
                TopMost = true
            };

            var label = new Label
            {
                Text = displayNumber.ToString(),
                ForeColor = Color.White,
                Font = new Font("Arial", 48, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            indicatorForm.Controls.Add(label);
            indicatorForm.Show();
            Application.DoEvents();

            Task.Delay(3000).ContinueWith(t =>
            {
                if (!indicatorForm.IsDisposed)
                {
                    try
                    {
                        indicatorForm.Invoke(new Action(() => indicatorForm.Close()));
                    }
                    catch { /* ignore */ }
                }
            });
        }

        #endregion

        #region Update Arduino Port

        private void UpdateArduinoPort()
        {
            if (_arduinoController != null && _arduinoController.Connect())
            {
                _arduinoController.Disconnect();
            }

            _settings.Port = textBoxPort.Text;
            SettingsManager.SaveSettings(_settings);

            _arduinoController = new ArduinoController(_settings.Port);
            Log.Information("Arduino port updated to {Port}.", _settings.Port);
        }

        #endregion
    }
}
