using System.IO.Ports;

namespace DofusHuntHelper;

public class ArduinoController
{
    private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArduinoController.log");
    private readonly List<string> _commandLog;
    private bool _isConnected;
    private readonly SerialPort _serialPort;

    public ArduinoController(string portName, int baudRate = 9600)
    {
        _serialPort = new SerialPort(portName, baudRate)
        {
            DtrEnable = true,
            RtsEnable = true
        };
        _commandLog = new List<string>();
        InitializeLogFile();
    }

    public event Action<string>? DataReceived;
    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<string>? ErrorOccurred;

    private void InitializeLogFile()
    {
        try
        {
            if (!File.Exists(_logFilePath)) File.Create(_logFilePath).Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur lors de la création du fichier de log : " + ex.Message);
        }
    }

    public bool Connect()
    {
        try
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                _isConnected = true;
                LogMessage("Arduino connecté.");
                Connected?.Invoke();
                return true;
            }

            HandleError("Le port série est déjà ouvert.");
        }
        catch (UnauthorizedAccessException)
        {
            HandleError(
                "Accès non autorisé au port série. Assurez-vous qu'aucune autre application n'utilise le port.");
        }
        catch (IOException ex)
        {
            HandleError("Erreur d'entrée/sortie : " + ex.Message);
        }
        catch (Exception ex)
        {
            HandleError("Erreur inconnue lors de la connexion à l'Arduino : " + ex.Message);
        }

        return false;
    }

    public void Disconnect()
    {
        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _isConnected = false;
                LogMessage("Arduino déconnecté.");
                Disconnected?.Invoke();
            }
            else
            {
                HandleError("Le port série est déjà fermé.");
            }
        }
        catch (Exception ex)
        {
            HandleError("Erreur lors de la déconnexion : " + ex.Message);
        }
    }

    public async Task SendSignalAsync(string signal)
    {
        if (_isConnected && _serialPort.IsOpen)
            try
            {
                _serialPort.Write(signal);
                LogMessage($"Signal envoyé : {signal}");
                _commandLog.Add(signal);
                await Task.Delay(100);
            }
            catch (InvalidOperationException)
            {
                HandleError("Le port série n'est pas ouvert.");
            }
            catch (TimeoutException)
            {
                HandleError("Délai d'attente dépassé lors de l'envoi du signal.");
            }
            catch (Exception ex)
            {
                HandleError("Erreur inattendue lors de l'envoi du signal : " + ex.Message);
            }
        else
            HandleError("Arduino non connecté.");
    }

    public async Task SendCommandAsync(string command, int delayMs = 100)
    {
        if (_isConnected && _serialPort.IsOpen)
            try
            {
                _serialPort.WriteLine(command);
                LogMessage($"Commande envoyée : {command}");
                _commandLog.Add(command);
                await Task.Delay(delayMs);
            }
            catch (InvalidOperationException)
            {
                HandleError("Le port série n'est pas ouvert.");
            }
            catch (TimeoutException)
            {
                HandleError("Délai d'attente dépassé lors de l'envoi de la commande.");
            }
            catch (Exception ex)
            {
                HandleError("Erreur inattendue lors de l'envoi de la commande : " + ex.Message);
            }
        else
            HandleError("Arduino non connecté.");
    }

    public void StartListening()
    {
        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.DataReceived += SerialPort_DataReceived;
                LogMessage("Écoute des signaux de l'Arduino...");
            }
            else
            {
                HandleError("Le port série n'est pas ouvert.");
            }
        }
        catch (Exception ex)
        {
            HandleError("Erreur lors du démarrage de l'écoute : " + ex.Message);
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var data = _serialPort.ReadExisting();
            LogMessage($"Signal reçu : {data}");
            DataReceived?.Invoke(data);
        }
        catch (Exception ex)
        {
            HandleError("Erreur lors de la réception des données : " + ex.Message);
        }
    }

    public void SendPulse(string signal, int durationMs)
    {
        Task.Run(async () =>
        {
            await SendSignalAsync(signal);
            await Task.Delay(durationMs);
            await SendSignalAsync("0");
        });
    }

    public async Task<bool> WaitForResponseAsync(string expectedResponse, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<bool>();

        void Handler(string data)
        {
            if (data.Contains(expectedResponse)) tcs.TrySetResult(true);
        }

        DataReceived += Handler;

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

        DataReceived -= Handler;

        if (completedTask == tcs.Task) return true;

        HandleError("Temps d'attente dépassé pour la réponse attendue.");
        return false;
    }

    public void LogCommandsToFile(string filePath)
    {
        try
        {
            File.WriteAllLines(filePath, _commandLog);
            LogMessage("Journal des commandes enregistré dans : " + filePath);
        }
        catch (UnauthorizedAccessException)
        {
            HandleError("Accès non autorisé pour enregistrer le fichier.");
        }
        catch (IOException ex)
        {
            HandleError("Erreur d'entrée/sortie lors de l'enregistrement du journal : " + ex.Message);
        }
        catch (Exception ex)
        {
            HandleError("Erreur inattendue lors de l'enregistrement du journal : " + ex.Message);
        }
    }

    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    private void HandleError(string errorMessage)
    {
        LogMessage(errorMessage);
        ErrorOccurred?.Invoke(errorMessage);
    }

    private void LogMessage(string message)
    {
        try
        {
            File.AppendAllText(_logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur lors de l'écriture dans le fichier de log : " + ex.Message);
        }
    }

    public async Task<bool> PingArduinoAsync(int timeoutMs = 1000)
    {
        try
        {
            await SendSignalAsync("PING");
            return await WaitForResponseAsync("PONG", timeoutMs);
        }
        catch (Exception ex)
        {
            HandleError("Erreur lors du ping de l'Arduino : " + ex.Message);
            return false;
        }
    }
}