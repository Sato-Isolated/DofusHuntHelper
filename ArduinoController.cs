using System.IO.Ports;

namespace DofusHuntHelper
{
    public class ArduinoController
    {
        private SerialPort _serialPort;
        private bool _isConnected;
        private List<string> _commandLog;

        public event Action<string>? DataReceived;
        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<string>? ErrorOccurred;

        public ArduinoController(string portName, int baudRate = 9600)
        {
            _serialPort = new SerialPort(portName, baudRate)
            {
                DtrEnable = true,  // Permet à certains modèles Arduino (comme le R4) de bien se réinitialiser
                RtsEnable = true
            };
            _commandLog = new List<string>();
        }

        public bool Connect()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _isConnected = true;
                    Console.WriteLine("Arduino connecté.");
                    Connected?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
            {
                HandleError("Erreur lors de la connexion à l'Arduino : " + ex.Message);
            }
            return false;
        }

        public void Disconnect()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _isConnected = false;
                Console.WriteLine("Arduino déconnecté.");
                Disconnected?.Invoke();
            }
        }

        public async Task SendSignalAsync(string signal)
        {
            if (_isConnected && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Write(signal);
                    Console.WriteLine($"Signal envoyé : {signal}");
                    _commandLog.Add(signal);
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    HandleError("Erreur lors de l'envoi du signal : " + ex.Message);
                }
            }
            else
            {
                HandleError("Arduino non connecté.");
            }
        }

        public async Task SendCommandAsync(string command, int delayMs = 100)
        {
            if (_isConnected && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.WriteLine(command);
                    Console.WriteLine($"Commande envoyée : {command}");
                    _commandLog.Add(command);
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    HandleError("Erreur lors de l'envoi de la commande : " + ex.Message);
                }
            }
        }

        public void StartListening()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.DataReceived += SerialPort_DataReceived;
                Console.WriteLine("Écoute des signaux de l'Arduino...");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _serialPort.ReadExisting();
            Console.WriteLine($"Signal reçu : {data}");
            DataReceived?.Invoke(data);
        }

        public void SendPulse(string signal, int durationMs)
        {
            Task.Run(async () =>
            {
                await SendSignalAsync(signal);
                await Task.Delay(durationMs);
                await SendSignalAsync("0"); // Envoyer un signal pour arrêter
            });
        }

        public async Task<bool> WaitForResponseAsync(string expectedResponse, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<bool>();

            void Handler(string data)
            {
                if (data.Contains(expectedResponse))
                {
                    tcs.TrySetResult(true);
                }
            }

            DataReceived += Handler;

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

            DataReceived -= Handler;

            return completedTask == tcs.Task;
        }

        public void LogCommandsToFile(string filePath)
        {
            try
            {
                File.WriteAllLines(filePath, _commandLog);
                Console.WriteLine("Journal des commandes enregistré dans : " + filePath);
            }
            catch (Exception ex)
            {
                HandleError("Erreur lors de l'enregistrement du journal des commandes : " + ex.Message);
            }
        }

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        private void HandleError(string errorMessage)
        {
            Console.WriteLine(errorMessage);
            ErrorOccurred?.Invoke(errorMessage);
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
}
