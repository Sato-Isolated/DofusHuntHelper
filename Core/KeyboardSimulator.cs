using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using Serilog;
using System.Windows.Forms;

/// <summary>
/// Classe simulant l’envoi de frappes clavier via l’API Windows SendInput.
/// </summary>
public class KeyboardSimulator : IDisposable
{
    #region WinAPI Imports

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    [Flags]
    public enum InputType : uint
    {
        INPUT_KEYBOARD = 1
    }

    [Flags]
    public enum KEYEVENTF : uint
    {
        KEYDOWN     = 0x0000,
        EXTENDEDKEY = 0x0001,
        KEYUP       = 0x0002,
        SCANCODE    = 0x0008,
        UNICODE     = 0x0004
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public KEYEVENTF dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public InputType type;
        public KEYBDINPUT ki;
        public long padding; // Padding spécifique pour 64-bit (8 octets)
    }

    #endregion

    #region Virtual Key Codes

    /// <summary>
    /// Énumération de touches virtuelles courantes (Windows).
    /// Certaines touches existent sous plusieurs variantes (gauche/droite).
    /// Ajoutez ou retirez selon vos besoins.
    /// </summary>
    public enum VirtualKey : ushort
    {
        // Région Alphanumérique de base
        VK_0 = 0x30,    // 0
        VK_1 = 0x31,    // 1
        VK_2 = 0x32,    // 2
        VK_3 = 0x33,    // 3
        VK_4 = 0x34,    // 4
        VK_5 = 0x35,    // 5
        VK_6 = 0x36,    // 6
        VK_7 = 0x37,    // 7
        VK_8 = 0x38,    // 8
        VK_9 = 0x39,    // 9

        VK_A = 0x41,    // A
        VK_B = 0x42,    // B
        VK_C = 0x43,    // C
        VK_D = 0x44,    // D
        VK_E = 0x45,    // E
        VK_F = 0x46,    // F
        VK_G = 0x47,    // G
        VK_H = 0x48,    // H
        VK_I = 0x49,    // I
        VK_J = 0x4A,    // J
        VK_K = 0x4B,    // K
        VK_L = 0x4C,    // L
        VK_M = 0x4D,    // M
        VK_N = 0x4E,    // N
        VK_O = 0x4F,    // O
        VK_P = 0x50,    // P
        VK_Q = 0x51,    // Q
        VK_R = 0x52,    // R
        VK_S = 0x53,    // S
        VK_T = 0x54,    // T
        VK_U = 0x55,    // U
        VK_V = 0x56,    // V
        VK_W = 0x57,    // W
        VK_X = 0x58,    // X
        VK_Y = 0x59,    // Y
        VK_Z = 0x5A,    // Z

        // Touches Système / Contrôle
        VK_RETURN  = 0x0D, // Entrée
        VK_ESCAPE  = 0x1B, // Echap
        VK_BACK    = 0x08, // Retour arrière
        VK_TAB     = 0x09, // Tabulation
        VK_SPACE   = 0x20, // Espace
        VK_SHIFT   = 0x10, // Maj
        VK_CONTROL = 0x11, // Ctrl
        VK_ALT     = 0x12, // Alt
        VK_PAUSE   = 0x13, // Pause
        VK_CAPS    = 0x14, // Verr Maj
        VK_LSHIFT  = 0xA0, // Maj gauche
        VK_RSHIFT  = 0xA1, // Maj droite
        VK_LCONTROL= 0xA2, // Ctrl gauche
        VK_RCONTROL= 0xA3, // Ctrl droite
        VK_LMENU   = 0xA4, // Alt gauche
        VK_RMENU   = 0xA5, // Alt droite
        VK_LWIN    = 0x5B, // Touche Windows gauche
        VK_RWIN    = 0x5C, // Touche Windows droite

        // Touches Fléchées
        VK_LEFT   = 0x25, // Flèche gauche
        VK_UP     = 0x26, // Flèche haut
        VK_RIGHT  = 0x27, // Flèche droite
        VK_DOWN   = 0x28, // Flèche bas

        // Touches Fonctions
        VK_F1  = 0x70,
        VK_F2  = 0x71,
        VK_F3  = 0x72,
        VK_F4  = 0x73,
        VK_F5  = 0x74,
        VK_F6  = 0x75,
        VK_F7  = 0x76,
        VK_F8  = 0x77,
        VK_F9  = 0x78,
        VK_F10 = 0x79,
        VK_F11 = 0x7A,
        VK_F12 = 0x7B,

        // Pavé numérique
        VK_NUMPAD0 = 0x60, // 0 (num pad)
        VK_NUMPAD1 = 0x61, // 1 (num pad)
        VK_NUMPAD2 = 0x62, // 2 (num pad)
        VK_NUMPAD3 = 0x63, // 3 (num pad)
        VK_NUMPAD4 = 0x64, // 4 (num pad)
        VK_NUMPAD5 = 0x65, // 5 (num pad)
        VK_NUMPAD6 = 0x66, // 6 (num pad)
        VK_NUMPAD7 = 0x67, // 7 (num pad)
        VK_NUMPAD8 = 0x68, // 8 (num pad)
        VK_NUMPAD9 = 0x69, // 9 (num pad)

        // Divers
        VK_OEM_PLUS  = 0xBB, // Touche + (selon clavier)
        VK_OEM_MINUS = 0xBD, // Touche - (selon clavier)
        VK_OEM_1     = 0xBA, // Touche ;: (clavier QWERTY)
        VK_OEM_2     = 0xBF, // Touche /? (clavier QWERTY)
        VK_OEM_3     = 0xC0, // Touche `~ (clavier QWERTY)
        VK_OEM_4     = 0xDB, // Touche [{ (clavier QWERTY)
        VK_OEM_5     = 0xDC, // Touche \| (clavier QWERTY)
        VK_OEM_6     = 0xDD, // Touche ]} (clavier QWERTY)
        VK_OEM_7     = 0xDE  // Touche '" (clavier QWERTY)
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Simule un appui + relâche (press) d’une touche virtuelle.
    /// </summary>
    public void SendKeyPress(VirtualKey key)
    {
        SendKeyDown(key);
        SendKeyUp(key);
    }

    /// <summary>
    /// Simule l’appui (key down) d’une touche virtuelle.
    /// </summary>
    public void SendKeyDown(VirtualKey key)
    {
        SendKeyboardInput(key, KEYEVENTF.KEYDOWN);
    }

    /// <summary>
    /// Simule le relâchement (key up) d’une touche virtuelle.
    /// </summary>
    public void SendKeyUp(VirtualKey key)
    {
        SendKeyboardInput(key, KEYEVENTF.KEYUP);
    }

    /// <summary>
    /// Simule une combinaison de touches, par exemple CTRL + C.
    /// </summary>
    public void SendCombination(VirtualKey modifier, VirtualKey key)
    {
        SendKeyDown(modifier);
        SendKeyPress(key);
        SendKeyUp(modifier);
    }

    /// <summary>
    /// Envoie du texte caractère par caractère en utilisant l’API SendInput.
    /// </summary>
    /// <param name="text">Le texte à envoyer.</param>
    /// <param name="delayBetweenKeys">Délai (en millisecondes) entre chaque caractère.</param>
    public void SendText(string text, int delayBetweenKeys = 10)
    {
        foreach (char c in text)
        {
            // Gestion d’une partie des caractères alphanumériques
            if (char.IsLetterOrDigit(c))
            {
                SendCharacter(c);
            }
            else
            {
                // Gérer quelques caractères spéciaux courants
                switch (c)
                {
                    case ' ':
                        SendKeyPress(VirtualKey.VK_SPACE);
                        break;
                    case '\t':
                        SendKeyPress(VirtualKey.VK_TAB);
                        break;
                    case '\n':
                    case '\r':
                        SendKeyPress(VirtualKey.VK_RETURN);
                        break;
                    default:
                        // Pour tout autre caractère, on envoie en UNICODE
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

    #endregion

    #region Private Implementation

    /// <summary>
    /// Envoie un caractère en utilisant les flags UNICODE (pour supporter
    /// les caractères hors ASCII, comme les accents, etc.).
    /// </summary>
    private void SendCharacter(char c)
    {
        ushort scanCode = c;
        // Envoi du key down en UNICODE
        SendKeyboardInput(0, KEYEVENTF.UNICODE, scanCode);
        // Envoi du key up en UNICODE
        SendKeyboardInput(0, KEYEVENTF.UNICODE | KEYEVENTF.KEYUP, scanCode);
    }

    /// <summary>
    /// Méthode générique qui envoie l’événement clavier (appui ou relâche).
    /// </summary>
    private void SendKeyboardInput(VirtualKey key, KEYEVENTF flags, ushort scanCode = 0)
    {
        INPUT input = new INPUT
        {
            type = InputType.INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = (ushort)key,
                wScan = scanCode,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = GetMessageExtraInfo()
            },
            padding = 0 // Initialisation explicite
        };

        INPUT[] inputs = new INPUT[] { input };
        int structSize = Marshal.SizeOf<INPUT>(); // Taille exacte en 64-bit

        uint result = SendInput(1, inputs, structSize);
        if (result != 1)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public void SendRealisticEnter()
    {
        // Version 1 : Simulation bas niveau avec flags étendus
        INPUT[] inputs = new INPUT[2];

        // Key DOWN
        inputs[0] = CreateKeyboardInput(
            VirtualKey.VK_RETURN,
            KEYEVENTF.EXTENDEDKEY | KEYEVENTF.SCANCODE,
            0x1C // Scan code étendu pour Enter
        );

        // Key UP
        inputs[1] = CreateKeyboardInput(
            VirtualKey.VK_RETURN,
            KEYEVENTF.EXTENDEDKEY | KEYEVENTF.SCANCODE | KEYEVENTF.KEYUP,
            0x1C
        );

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    private INPUT CreateKeyboardInput(VirtualKey key, KEYEVENTF flags, ushort scanCode)
    {
        return new INPUT
        {
            type = InputType.INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = 0, // Désactive le virtual key
                wScan = scanCode,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = GetMessageExtraInfo()
            }
        };
    }

    #endregion

    #region IDisposable Implementation

    private bool disposed = false;

    /// <summary>
    /// Détruit l’objet de manière contrôlée.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Libère les ressources non managées si nécessaire.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            // Libérer des ressources ici si besoin...
            disposed = true;
        }
    }

    ~KeyboardSimulator()
    {
        Dispose(false);
    }

    #endregion
}
