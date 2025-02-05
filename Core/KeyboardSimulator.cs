using System.ComponentModel;
using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054

namespace DofusHuntHelper.Core;

public sealed class KeyboardSimulator : IDisposable
{
    #region Virtual Key Codes

    public enum VirtualKey : ushort
    {
        VkV = 0x56,
        VkReturn = 0x0D,
        VkTab = 0x09,
        VkSpace = 0x20,
        VkControl = 0x11
    }

    #endregion

    #region WinAPI Imports

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    [Flags]
    private enum InputType : uint
    {
        InputKeyboard = 1
    }

    [Flags]
    private enum Keyeventf : uint
    {
        Keydown = 0x0000,
        Extendedkey = 0x0001,
        Keyup = 0x0002,
        Scancode = 0x0008,
        Unicode = 0x0004
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Keybdinput
    {
        public ushort wVk;
        public ushort wScan;
        public Keyeventf dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public InputType type;
        public Keybdinput ki;
        public long padding; // Padding spécifique pour 64-bit (8 octets)
    }

    #endregion

    #region Public Methods

    private static void SendKeyPress(VirtualKey key)
    {
        SendKeyboardInput(key, Keyeventf.Keydown);
        SendKeyboardInput(key, Keyeventf.Keyup);
    }

    private static void SendKeyDown(VirtualKey key)
    {
        SendKeyboardInput(key, Keyeventf.Keydown);
    }

    private static void SendKeyUp(VirtualKey key)
    {
        SendKeyboardInput(key, Keyeventf.Keyup);
    }

    public static void SendCombination(VirtualKey modifier, VirtualKey key)
    {
        SendKeyDown(modifier);
        SendKeyPress(key);
        SendKeyUp(modifier);
    }

    public static void SendText(string text, int delayBetweenKeys = 10)
    {
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
                SendCharacter(c);
            else
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

            if (delayBetweenKeys > 0) Thread.Sleep(delayBetweenKeys);
        }
    }

    #endregion

    #region Private Implementation

    private static void SendCharacter(char c)
    {
        ushort scanCode = c;
        SendKeyboardInput(0, Keyeventf.Unicode, scanCode);
        SendKeyboardInput(0, Keyeventf.Unicode | Keyeventf.Keyup, scanCode);
    }

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
        if (result != 1) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public static void SendRealisticEnter()
    {
        Span<Input> inputs =
        [
            CreateKeyboardInput(Keyeventf.Extendedkey | Keyeventf.Scancode, 0x1C),
            CreateKeyboardInput(Keyeventf.Extendedkey | Keyeventf.Scancode | Keyeventf.Keyup, 0x1C)
        ];
        var result = SendInput(2, inputs.ToArray(), Marshal.SizeOf<Input>());
        if (result != 2) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

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

    #endregion

    #region IDisposable Implementation

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed) _disposed = true;
    }

    ~KeyboardSimulator()
    {
        Dispose(false);
    }

    #endregion
}