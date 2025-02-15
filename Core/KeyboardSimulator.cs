using System.ComponentModel;
using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054

namespace DofusHuntHelper.Core
{
    /// <summary>
    /// Fournit des méthodes pour simuler des frappes de touches du clavier
    /// (appui, relâchement, envoi de textes, combinaisons de touches, etc.)
    /// à l'aide de fonctions WinAPI.
    /// </summary>
    public sealed class KeyboardSimulator : IDisposable
    {
        #region Virtual Key Codes

        /// <summary>
        /// Représente un ensemble de codes de touches virtuelles (Virtual-Key) utilisés par l'API Win32.
        /// </summary>
        public enum VirtualKey : ushort
        {
            /// <summary>
            /// Touche "V".
            /// </summary>
            VkV = 0x56,

            /// <summary>
            /// Touche "Entrée".
            /// </summary>
            VkReturn = 0x0D,

            /// <summary>
            /// Touche "Tabulation".
            /// </summary>
            VkTab = 0x09,

            /// <summary>
            /// Touche "Espace".
            /// </summary>
            VkSpace = 0x20,

            /// <summary>
            /// Touche "Contrôle".
            /// </summary>
            VkControl = 0x11
        }

        #endregion Virtual Key Codes

        #region WinAPI Imports

        // Déclarations de fonctions et structures Win32
        // pour la simulation de l'appui/clavier via SendInput

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        /// <summary>
        /// Enum indiquant le type d'input (ici, seulement "InputKeyboard").
        /// </summary>
        [Flags]
        private enum InputType : uint
        {
            InputKeyboard = 1
        }

        /// <summary>
        /// Enum indiquant l'état de la touche (enfoncée, relâchée, etc.).
        /// </summary>
        [Flags]
        private enum Keyeventf : uint
        {
            /// <summary>
            /// Appui de touche par défaut.
            /// </summary>
            Keydown = 0x0000,

            /// <summary>
            /// Touche étendue.
            /// </summary>
            Extendedkey = 0x0001,

            /// <summary>
            /// Relâchement de la touche.
            /// </summary>
            Keyup = 0x0002,

            /// <summary>
            /// Clé représentée par un code de balayage (scan code).
            /// </summary>
            Scancode = 0x0008,

            /// <summary>
            /// Clé représentée par un caractère Unicode.
            /// </summary>
            Unicode = 0x0004
        }

        /// <summary>
        /// Structure décrivant un input clavier pour la fonction WinAPI SendInput.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Keybdinput
        {
            public ushort wVk;
            public ushort wScan;
            public Keyeventf dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Structure enveloppant les informations d’input
        /// envoyées à SendInput (ici, type = InputKeyboard).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public InputType type;
            public Keybdinput ki;
            public long padding; // Padding spécifique pour 64 bits (8 octets).
        }

        #endregion WinAPI Imports

        #region Public Methods

        /// <summary>
        /// Envoie une pression et un relâchement successif de la touche virtuelle spécifiée.
        /// </summary>
        /// <param name="key">Touche à presser et relâcher.</param>
        private static void SendKeyPress(VirtualKey key)
        {
            SendKeyboardInput(key, Keyeventf.Keydown);
            SendKeyboardInput(key, Keyeventf.Keyup);
        }

        /// <summary>
        /// Simule l’appui (sans relâchement) de la touche virtuelle spécifiée.
        /// </summary>
        /// <param name="key">Touche à maintenir enfoncée.</param>
        private static void SendKeyDown(VirtualKey key)
        {
            SendKeyboardInput(key, Keyeventf.Keydown);
        }

        /// <summary>
        /// Simule le relâchement de la touche virtuelle spécifiée.
        /// </summary>
        /// <param name="key">Touche à relâcher.</param>
        private static void SendKeyUp(VirtualKey key)
        {
            SendKeyboardInput(key, Keyeventf.Keyup);
        }

        /// <summary>
        /// Simule l’appui simultané (combinaison) d’une touche <paramref name="modifier"/>
        /// et d’une touche <paramref name="key"/>, puis relâche la touche modificatrice.
        /// </summary>
        /// <param name="modifier">La touche modificatrice (ex: Ctrl).</param>
        /// <param name="key">La touche principale (ex: 'V').</param>
        public static void SendCombination(VirtualKey modifier, VirtualKey key)
        {
            SendKeyDown(modifier);
            SendKeyPress(key);
            SendKeyUp(modifier);
        }

        /// <summary>
        /// Simule la frappe d’un texte complet, caractère par caractère.
        /// Gère automatiquement l’espace, la tabulation et la touche entrée.
        /// </summary>
        /// <param name="text">Chaîne de caractères à envoyer.</param>
        /// <param name="delayBetweenKeys">
        /// Délai en millisecondes entre chaque caractère (par défaut 10 ms).
        /// Peut être mis à 0 pour aller plus vite.
        /// </param>
        public static void SendText(string text, int delayBetweenKeys = 10)
        {
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    SendCharacter(c);
                }
                else
                {
                    switch (c)
                    {
                        case ' ':
                            SendKeyPress(VirtualKey.VkSpace);
                            break;

                        case '\t':
                            SendKeyPress(VirtualKey.VkTab);
                            break;

                        case '\n':
                        case '\r':
                            SendKeyPress(VirtualKey.VkReturn);
                            break;

                        default:
                            SendCharacter(c);
                            break;
                    }
                }

                if (delayBetweenKeys > 0)
                {
                    Thread.Sleep(delayBetweenKeys);
                }
            }
        }

        #endregion Public Methods

        #region Private Implementation

        /// <summary>
        /// Envoie un caractère Unicode à l’aide d’un scan code.
        /// </summary>
        /// <param name="c">Caractère à envoyer.</param>
        private static void SendCharacter(char c)
        {
            ushort scanCode = c;
            // Keydown Unicode
            SendKeyboardInput(0, Keyeventf.Unicode, scanCode);
            // Keyup Unicode
            SendKeyboardInput(0, Keyeventf.Unicode | Keyeventf.Keyup, scanCode);
        }

        /// <summary>
        /// Enveloppe l’appel à <see cref="SendInput"/> pour un key event unique.
        /// </summary>
        /// <param name="key">La touche virtuelle à enfoncer/relâcher (0 si mode Unicode).</param>
        /// <param name="flags">
        /// Indique l’action : appui (Keydown), relâchement (Keyup), Unicode, etc.
        /// </param>
        /// <param name="scanCode">Code de balayage (utile pour les caractères Unicode).</param>
        /// <exception cref="Win32Exception">
        /// Lancée si l’appel à <see cref="SendInput"/> échoue.
        /// </exception>
        private static void SendKeyboardInput(VirtualKey key, Keyeventf flags, ushort scanCode = 0)
        {
            var input = new Input
            {
                type = InputType.InputKeyboard,
                ki = new Keybdinput
                {
                    wVk = (ushort)key,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                },
                padding = 0
            };

            Span<Input> inputs = [input];
            var structSize = Marshal.SizeOf<Input>();

            var result = SendInput(1, inputs.ToArray(), structSize);
            if (result != 1)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Simule un "Enter" en utilisant le scan code 0x1C,
        /// ce qui peut reproduire de manière plus "réaliste" l'appui.
        /// </summary>
        /// <exception cref="Win32Exception">Si l’appel à <see cref="SendInput"/> échoue.</exception>
        public static void SendRealisticEnter()
        {
            Span<Input> inputs =
            [
                CreateKeyboardInput(Keyeventf.Extendedkey | Keyeventf.Scancode, 0x1C),
                CreateKeyboardInput(Keyeventf.Extendedkey | Keyeventf.Scancode | Keyeventf.Keyup, 0x1C)
            ];

            var result = SendInput(2, inputs.ToArray(), Marshal.SizeOf<Input>());
            if (result != 2)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Construit une structure <see cref="Input"/> avec les flags et le scan code spécifiés,
        /// pour un événement clavier simulé.
        /// </summary>
        /// <param name="flags">Flags indiquant le type d’action (appui, relâchement, etc.).</param>
        /// <param name="scanCode">Le code de balayage à envoyer.</param>
        /// <returns>Une structure <see cref="Input"/> initialisée.</returns>
        private static Input CreateKeyboardInput(Keyeventf flags, ushort scanCode)
        {
            return new Input
            {
                type = InputType.InputKeyboard,
                ki = new Keybdinput
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = GetMessageExtraInfo()
                }
            };
        }

        #endregion Private Implementation

        #region IDisposable Implementation

        private bool _disposed;

        /// <summary>
        /// Libère les ressources associées au <see cref="KeyboardSimulator"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Méthode de finalisation de la classe, appelée implicitement par le GC si
        /// <see cref="Dispose()"/> n’a pas déjà été invoquée.
        /// </summary>
        ~KeyboardSimulator()
        {
            Dispose(false);
        }

        /// <summary>
        /// Libère les ressources si nécessaire (modèle standard du pattern <see cref="IDisposable"/>).
        /// </summary>
        /// <param name="disposing">Indique si l’appel provient d’un appel à <see cref="Dispose()"/> (true) ou du GC (false).</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Insérer ici la libération de ressources managées si nécessaire.
                // Actuellement, il n’y a pas de ressources managées à libérer.

                // Marquer l’objet comme disposé.
                _disposed = true;
            }
        }

        #endregion IDisposable Implementation
    }
}