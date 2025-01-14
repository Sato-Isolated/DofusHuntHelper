using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Serilog;

namespace DofusHuntHelper.Arduino
{
    public class ArduinoController : IDisposable
    {
        private readonly List<string> _commandLog;
        private bool _isConnected;
        private readonly SerialPort _serialPort;

        public event Action<string>? DataReceived;
        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<string>? ErrorOccurred;

        public ArduinoController(string portName, int baudRate = 9600)
        {
            if (string.IsNullOrEmpty(portName))
                throw new ArgumentException("Le nom du port ne peut pas être vide", nameof(portName));

            _commandLog = new List<string>();

            _serialPort = new SerialPort(portName, baudRate)
            {
                DtrEnable = true,
                RtsEnable = true
            };

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
                    Log.Information("[ArduinoController] Connected on port {Port}.", _serialPort.PortName);

                    Connected?.Invoke();
                    return true;
                }
                HandleError("Le port série est déjà ouvert.");
            }
            catch (Exception ex)
            {
                HandleError("Erreur lors de la connexion à l'Arduino : " + ex.Message);
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
                    Log.Information("[ArduinoController] Disconnected from port {Port}.", _serialPort.PortName);

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
            await WriteAsync(signal, addNewLine: false, delayMs: 100);
        }

        public async Task SendCommandAsync(string command, int delayMs = 100)
        {
            await WriteAsync(command, addNewLine: true, delayMs);
        }

        private async Task WriteAsync(string payload, bool addNewLine, int delayMs)
        {
            if (_isConnected && _serialPort.IsOpen)
            {
                try
                {
                    if (addNewLine)
                        _serialPort.WriteLine(payload);
                    else
                        _serialPort.Write(payload);

                    LogMessage($"Écriture : {payload}");
                    Log.Debug("[ArduinoController] Write: {Payload}", payload);

                    _commandLog.Add(payload);

                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    HandleError("Erreur lors de l'écriture : " + ex.Message);
                }
            }
            else
            {
                HandleError("Arduino non connecté ou port fermé.");
            }
        }

        public void StartListening()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DataReceived += SerialPort_DataReceived;
                    LogMessage("Écoute des signaux de l'Arduino...");
                    Log.Information("[ArduinoController] Start listening on port {Port}.", _serialPort.PortName);
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

        public void StopListening()
        {
            if (_serialPort != null)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
            }
            LogMessage("Arrêt de l'écoute des signaux de l'Arduino.");
            Log.Information("[ArduinoController] Stop listening on port {Port}.", _serialPort.PortName);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var data = _serialPort.ReadExisting();
                LogMessage($"Signal reçu : {data}");
                Log.Debug("[ArduinoController] DataReceived: {Data}", data);

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
                if (data.Contains(expectedResponse))
                    tcs.TrySetResult(true);
            }

            DataReceived += Handler;

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

            DataReceived -= Handler;

            if (completedTask == tcs.Task)
                return true;

            HandleError("Temps d'attente dépassé pour la réponse attendue.");
            return false;
        }

        private void HandleError(string errorMessage)
        {
            LogMessage(errorMessage);
            Log.Error("[ArduinoController] {ErrorMessage}", errorMessage);
            ErrorOccurred?.Invoke(errorMessage);
        }

        private void LogMessage(string message)
        {
            try
            {
                Log.Information("[ArduinoController] {Message}", message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ArduinoController] Error writing to log file");
            }
        }

        public void Dispose()
        {
            StopListening();

            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
            }
        }
    }
}
