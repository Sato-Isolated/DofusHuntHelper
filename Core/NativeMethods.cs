using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054
namespace DofusHuntHelper.Core;

internal static class NativeMethods
{
    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Constantes pour les événements de la souris
    public const int MouseeventfLeftdown = 0x0002;
    public const int MouseeventfLeftup = 0x0004;
    public const int WhMouseLl = 14;
    public const int WmLbuttondown = 0x0201;

    // Importation des fonctions de la bibliothèque user32.dll
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // Méthode utilitaire pour un clic gauche complet
    public static void PerformLeftClick(int x, int y)
    {
        mouse_event(MouseeventfLeftdown, x, y, 0, 0);
        mouse_event(MouseeventfLeftup, x, y, 0, 0);
    }

    // Struct pour la position du curseur
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }
}