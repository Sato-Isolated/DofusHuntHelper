using System.Diagnostics;

namespace DofusHuntHelper;

public partial class GUI : Form
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private readonly NativeMethods.HookProc hookProc;
    private readonly string initialClipboardText = string.Empty;
    private ArduinoController arduinoPort;
    private Thread? clipboardThread;

    private IntPtr hookId = IntPtr.Zero;
    private bool isRunning;

    public GUI()
    {
        InitializeComponent();
        LoadCoordinates();
        hookProc = MouseHookCallback;
        arduinoPort = new ArduinoController(textBox1.Text);
        textBox1.TextChanged += (s, e) => UpdateArduinoPort();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        hookId = NativeMethods.SetWindowsHookEx(WH_MOUSE_LL, hookProc, IntPtr.Zero, 0);
        label1.Text = "Cliquez avec le bouton gauche pour capturer les coordonnées...";
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_LBUTTONDOWN)
            if (NativeMethods.GetCursorPos(out var point))
            {
                var coordinates = $"X={point.X}, Y={point.Y}";
                label1.Text = "Coordonnée: " + coordinates;
                richTextBox1.AppendText($"Coordonnée sauvegardée X: {point.X} Y: {point.Y}\n");
                SaveCoordinates(point.X, point.Y);
                NativeMethods.UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
                MessageBox.Show("Coordonnées sauvegardées avec succès !", "Confirmation", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

        return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private void SaveCoordinates(int x, int y)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coordinates.ini");
        var port = textBox1.Text;
        File.WriteAllText(filePath, $"X={x}\nY={y}\nPort={port}");
    }

    private void LoadCoordinates()
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coordinates.ini");
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length >= 3)
                {
                    int.TryParse(lines[0].Split('=')[1], out var savedX);
                    int.TryParse(lines[1].Split('=')[1], out var savedY);
                    var savedPort = lines[2].Split('=')[1];

                    textBox1.Text = savedPort;
                    label1.Text = $"Coordonnée: X={savedX}, Y={savedY}";
                    richTextBox1.AppendText($"Coordonnée chargée X: {savedX} Y: {savedY}, Port: {savedPort}\n");
                }
            }
        }
        catch
        {
            MessageBox.Show("Erreur lors du chargement des coordonnées.", "Erreur", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void button2_Click(object sender, EventArgs e)
    {
        if (!isRunning)
        {
            isRunning = true;
            button2.Text = "Stop";

            if (!arduinoPort.Connect())
            {
                MessageBox.Show("Échec de la connexion à l'Arduino.", "Erreur", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            clipboardThread = new Thread(() =>
            {
                var lastClipboardText = initialClipboardText;

                while (isRunning)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            var clipboardText = Clipboard.GetText();

                            if (clipboardText != lastClipboardText && clipboardText.Contains("/travel"))
                            {
                                lastClipboardText = clipboardText;
                                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "coordinates.ini");
                                if (File.Exists(filePath))
                                {
                                    var lines = File.ReadAllLines(filePath);
                                    int.TryParse(lines[0].Split('=')[1], out var savedX);
                                    int.TryParse(lines[1].Split('=')[1], out var savedY);
                                    Invoke(new Action(async () =>
                                    {
                                        var selectedIndex = comboBox1.SelectedIndex;
                                        if (selectedIndex >= 0 && selectedIndex < Screen.AllScreens.Length)
                                        {
                                            var screenBounds = Screen.AllScreens[selectedIndex].Bounds;
                                            Cursor.Position = new Point(screenBounds.X + savedX,
                                                screenBounds.Y + savedY);

                                            var processes = Process.GetProcessesByName("Dofus");
                                            if (processes.Length > 0)
                                            {
                                                var handle = processes[0].MainWindowHandle;
                                                NativeMethods.SetForegroundWindow(handle);

                                                // Mouse click
                                                NativeMethods.mouse_event(
                                                    NativeMethods.MOUSEEVENTF_LEFTDOWN |
                                                    NativeMethods.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                                                // Refocus on the Dofus window
                                                NativeMethods.SetForegroundWindow(handle);

                                                // Paste clipboard content
                                                SendKeys.SendWait("^v");

                                                // Refocus on the Dofus window
                                                NativeMethods.SetForegroundWindow(handle);

                                                await arduinoPort.SendCommandAsync("1");
                                                Thread.Sleep(200);
                                                await arduinoPort.SendCommandAsync("1");

                                                richTextBox1.AppendText(
                                                    "Action exécutée avec succès sur l'écran sélectionné !\n");
                                            }
                                        }
                                    }));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() => MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButtons.OK,
                            MessageBoxIcon.Error)));
                    }

                    Thread.Sleep(500);
                }
            });

            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.Start();
        }
        else
        {
            isRunning = false;
            button2.Text = "Start";
            clipboardThread?.Join();
            arduinoPort.Disconnect();
        }
    }


    private void GUI_Load(object sender, EventArgs e)
    {
        var monitorNames = Screen.AllScreens.Select((s, index) => $"DISPLAY{index + 1}").ToList();

        foreach (var monitor in monitorNames) comboBox1.Items.Add(monitor);

        if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;
    }

    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedIndex = comboBox1.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < Screen.AllScreens.Length)
            ShowMonitorIndicator(selectedIndex, selectedIndex + 1);
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

        Task.Delay(3000).ContinueWith(t => indicatorForm.Invoke(() => indicatorForm.Close()));
    }

    private void UpdateArduinoPort()
    {
        if (arduinoPort != null && arduinoPort.Connect()) arduinoPort.Disconnect();

        arduinoPort = new ArduinoController(textBox1.Text);
    }
}