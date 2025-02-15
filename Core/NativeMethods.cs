using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054

namespace DofusHuntHelper.Core
{
    /// <summary>
    /// Fournit un ensemble de méthodes, delegates et constantes pour appeler
    /// les fonctions de l'API Windows (user32.dll) liées à la gestion de la souris
    /// et des hooks système.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Représente la signature d'une fonction de rappel (callback)
        /// pour les hooks Windows (ex : WH_MOUSE_LL).
        /// </summary>
        /// <param name="nCode">Code du hook, indiquant le type de traitement nécessaire.</param>
        /// <param name="wParam">Paramètre additionnel (en général, le message Windows).</param>
        /// <param name="lParam">Paramètre additionnel (pointeur vers des informations sur l'événement).</param>
        /// <returns>L'adresse du prochain hook dans la chaîne (gérée par <see cref="CallNextHookEx"/>).</returns>
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Constantes pour l'API

        /// <summary>
        /// Événement : Bouton gauche de la souris enfoncé (down).
        /// </summary>
        public const int MouseeventfLeftdown = 0x0002;

        /// <summary>
        /// Événement : Bouton gauche de la souris relâché (up).
        /// </summary>
        public const int MouseeventfLeftup = 0x0004;

        /// <summary>
        /// Code pour installer un hook de souris de bas niveau (WH_MOUSE_LL).
        /// </summary>
        public const int WhMouseLl = 14;

        /// <summary>
        /// Code de message Windows pour le clic gauche de la souris (WM_LBUTTONDOWN).
        /// </summary>
        public const int WmLbuttondown = 0x0201;

        #endregion Constantes pour l'API

        #region Fonctions P/Invoke (user32.dll)

        /// <summary>
        /// Installe un hook dans la chaîne des hooks Windows.
        /// </summary>
        /// <param name="idHook">
        /// Type de hook à installer (ex: <see cref="WhMouseLl"/> pour la souris de bas niveau).
        /// </param>
        /// <param name="lpfn">Fonction de rappel (callback) appelée lors du déclenchement du hook.</param>
        /// <param name="hMod">
        /// Handle du module qui contient la fonction de rappel. Peut être <c>IntPtr.Zero</c> si non géré.
        /// </param>
        /// <param name="dwThreadId">ID du thread auquel le hook est associé (0 = tous les threads).</param>
        /// <returns>Un handle vers le hook installé (ou <c>IntPtr.Zero</c> en cas d'erreur).</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// Désinstalle un hook précédemment installé.
        /// </summary>
        /// <param name="hhk">Handle vers le hook à désinstaller.</param>
        /// <returns>
        /// <c>true</c> si la désinstallation a réussi, <c>false</c> sinon.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Appelle le prochain hook dans la chaîne après celui donné.
        /// </summary>
        /// <param name="hhk">Handle vers le hook courant (ou <c>IntPtr.Zero</c>).</param>
        /// <param name="nCode">Code du hook.</param>
        /// <param name="wParam">Paramètre additionnel (message Windows).</param>
        /// <param name="lParam">Paramètre additionnel (informations sur l'événement).</param>
        /// <returns>Le résultat du prochain hook dans la chaîne.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Récupère la position actuelle du curseur de la souris.
        /// </summary>
        /// <param name="lpPoint">Structure <see cref="Point"/> qui recevra la position (X,Y).</param>
        /// <returns>
        /// <c>true</c> si la position est obtenue avec succès, <c>false</c> sinon.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetCursorPos(out Point lpPoint);

        /// <summary>
        /// Envoie un événement de souris au système (déplacement, clic, etc.).
        /// </summary>
        /// <param name="dwFlags">Type d'événement (ex : <see cref="MouseeventfLeftdown"/>).</param>
        /// <param name="dx">Position en X (relative ou absolue selon mode).</param>
        /// <param name="dy">Position en Y (relative ou absolue selon mode).</param>
        /// <param name="cButtons">Nombre de clics de la souris (généralement 0).</param>
        /// <param name="dwExtraInfo">Informations supplémentaires (0 par défaut).</param>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// Met la fenêtre associée à <paramref name="hWnd"/> au premier plan (focus).
        /// </summary>
        /// <param name="hWnd">Handle de la fenêtre.</param>
        /// <returns>
        /// <c>true</c> si la fenêtre a bien été mise au premier plan, <c>false</c> sinon.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion Fonctions P/Invoke (user32.dll)

        #region Méthode utilitaire

        /// <summary>
        /// Effectue un clic gauche à l'emplacement (x, y).
        /// </summary>
        /// <param name="x">Coordonnée en X.</param>
        /// <param name="y">Coordonnée en Y.</param>
        public static void PerformLeftClick(int x, int y)
        {
            mouse_event(MouseeventfLeftdown, x, y, 0, 0);
            mouse_event(MouseeventfLeftup, x, y, 0, 0);
        }

        #endregion Méthode utilitaire

        #region Structures internes

        /// <summary>
        /// Représente une position 2D (X, Y) de la souris ou d'un point à l'écran.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            /// <summary>
            /// Position en X.
            /// </summary>
            public int X;

            /// <summary>
            /// Position en Y.
            /// </summary>
            public int Y;
        }

        #endregion Structures internes
    }
}