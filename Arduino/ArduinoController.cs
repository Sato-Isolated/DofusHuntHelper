using System.Diagnostics;
using System.IO.Ports;

namespace DofusHuntHelper.Arduino
{
    /// <summary>
    /// Représente un contrôleur pour un Arduino connecté via un port série.
    /// Permet de gérer la connexion, l’envoi de commandes et la réception de signaux.
    /// </summary>
    public class ArduinoController : IDisposable
    {
        /// <summary>
        /// Historique des commandes envoyées.
        /// </summary>
        private readonly List<string> _commandLog;

        /// <summary>
        /// Référence au port série utilisé pour communiquer avec l’Arduino.
        /// </summary>
        private readonly SerialPort _serialPort;

        /// <summary>
        /// Indique si l’Arduino est actuellement connecté.
        /// </summary>
        private bool _isConnected;

        /// <summary>
        /// Initialise une nouvelle instance du <see cref="ArduinoController"/>.
        /// </summary>
        /// <param name="portName">Le nom du port série (ex : "COM3").</param>
        /// <param name="baudRate">Le débit en bauds (par défaut : 9600).</param>
        /// <exception cref="ArgumentException">Lève une exception si le nom du port est vide.</exception>
        public ArduinoController(string portName, int baudRate = 9600)
        {
            if (string.IsNullOrEmpty(portName))
                throw new ArgumentException(@"Le nom du port ne peut pas être vide", nameof(portName));

            _commandLog = new List<string>();

            _serialPort = new SerialPort(portName, baudRate)
            {
                DtrEnable = true,
                RtsEnable = true
            };
        }

        /// <summary>
        /// Détruit l’instance du <see cref="ArduinoController"/> et libère les ressources allouées.
        /// </summary>
        public void Dispose()
        {
            // Arrête l’écoute et ferme le port série avant de libérer les ressources.
            StopListening();

            if (_serialPort.IsOpen)
                _serialPort.Close();
            _serialPort.Dispose();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Déclenché lorsqu’une donnée est reçue depuis l’Arduino (chaîne de caractères).
        /// </summary>
        public event Action<string>? DataReceived;

        /// <summary>
        /// Déclenché lorsque la connexion à l’Arduino est établie.
        /// </summary>
        public event Action? Connected;

        /// <summary>
        /// Déclenché lorsque la connexion à l’Arduino est interrompue ou que le port est fermé.
        /// </summary>
        public event Action? Disconnected;

        /// <summary>
        /// Déclenché lorsqu’une erreur survient (message d’erreur en paramètre).
        /// </summary>
        public event Action<string>? ErrorOccurred;

        /// <summary>
        /// Ouvre la connexion série avec l’Arduino.
        /// </summary>
        /// <returns>
        /// <c>true</c> si la connexion a pu être établie avec succès,
        /// <c>false</c> sinon.
        /// </returns>
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
            catch (Exception ex)
            {
                HandleError("Erreur lors de la connexion à l'Arduino : " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Ferme la connexion série avec l’Arduino.
        /// </summary>
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

        /// <summary>
        /// Envoie un signal (chaîne de caractères) à l’Arduino, sans saut de ligne.
        /// </summary>
        /// <param name="signal">Le signal à envoyer (ex : "1" ou "0").</param>
        public async Task SendSignalAsync(string signal)
        {
            await WriteAsync(signal, addNewLine: false, delayMs: 100);
        }

        /// <summary>
        /// Envoie une commande (chaîne de caractères) à l’Arduino, avec saut de ligne.
        /// </summary>
        /// <param name="command">La commande à envoyer (ex : "LED_ON").</param>
        /// <param name="delayMs">Temps d’attente (en millisecondes) après l’écriture, par défaut 100 ms.</param>
        public async Task SendCommandAsync(string command, int delayMs = 100)
        {
            await WriteAsync(command, addNewLine: true, delayMs);
        }

        /// <summary>
        /// Commence à écouter les données en provenance de l’Arduino.
        /// Souscrit à l’événement <see cref="SerialPort.DataReceived"/>.
        /// </summary>
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

        /// <summary>
        /// Envoie un signal, attend un certain délai, puis envoie un "0" (par défaut), simulant une impulsion (pulse).
        /// </summary>
        /// <param name="signal">Le signal à envoyer (ex : "1").</param>
        /// <param name="durationMs">Durée de l’impulsion, en millisecondes.</param>
        public void SendPulse(string signal, int durationMs)
        {
            Task.Run(async () =>
            {
                await SendSignalAsync(signal);
                await Task.Delay(durationMs);
                await SendSignalAsync("0");
            });
        }

        /// <summary>
        /// Attend une réponse spécifique de l’Arduino dans un délai imparti.
        /// </summary>
        /// <param name="expectedResponse">La réponse attendue (sous forme de chaîne).</param>
        /// <param name="timeoutMs">Temps maximum d’attente en millisecondes.</param>
        /// <returns>
        /// <c>true</c> si la réponse attendue est reçue avant expiration du délai,
        /// <c>false</c> sinon.
        /// </returns>
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

        /// <summary>
        /// Méthode privée appelée pour écrire une commande sur le port série,
        /// avec ou sans saut de ligne, et attendre un délai donné.
        /// </summary>
        /// <param name="payload">Le contenu à écrire (signal ou commande).</param>
        /// <param name="addNewLine">Indique si un saut de ligne doit être ajouté en fin d’écriture.</param>
        /// <param name="delayMs">Temps d’attente (ms) après l’écriture.</param>
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

                    _commandLog.Add(payload);

                    // Petite pause pour laisser à l’Arduino le temps de traiter la commande.
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

        /// <summary>
        /// Méthode privée pour arrêter l’écoute des données en provenance de l’Arduino.
        /// Désabonne l’événement <see cref="SerialPort.DataReceived"/>.
        /// </summary>
        private void StopListening()
        {
            _serialPort.DataReceived -= SerialPort_DataReceived;
            LogMessage("Arrêt de l'écoute des signaux de l'Arduino.");
        }

        /// <summary>
        /// Méthode déclenchée lorsqu’une donnée arrive sur le port série.
        /// </summary>
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

        /// <summary>
        /// Méthode privée pour gérer les erreurs et lever l’événement <see cref="ErrorOccurred"/>.
        /// </summary>
        /// <param name="errorMessage">Message d’erreur à consigner et à envoyer aux abonnés.</param>
        private void HandleError(string errorMessage)
        {
            LogMessage(errorMessage);
            ErrorOccurred?.Invoke(errorMessage);
        }

        /// <summary>
        /// Méthode interne pour écrire des messages dans la log.
        /// </summary>
        /// <param name="message">Message à enregistrer.</param>
        private static void LogMessage(string message)
        {
            try
            {
                Debug.Print("[ArduinoController] {0}", message);
            }
            catch (Exception ex)
            {
                Debug.Print("[ArduinoController] Error writing to log file {0}", ex);
            }
        }
    }
}