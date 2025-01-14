using System;
using System.Threading;
using System.Windows.Forms;
using Serilog;

namespace DofusHuntHelper.Core
{
    public class ClipboardWatcher : IDisposable
    {
        private Thread? _watcherThread;
        private bool _isRunning;
        private string _lastText = string.Empty;
        private readonly int _intervalMs;

        public event Action<string>? OnClipboardTextChanged;
        public event Action<Exception>? OnError;

        public ClipboardWatcher(int intervalMs = 500)
        {
            _intervalMs = intervalMs;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _watcherThread = new Thread(WatchClipboard);
            _watcherThread.SetApartmentState(ApartmentState.STA);
            _watcherThread.Start();
            Log.Information("ClipboardWatcher started with interval {Interval} ms", _intervalMs);
        }

        private void WatchClipboard()
        {
            while (_isRunning)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        var currentText = Clipboard.GetText();
                        if (currentText != _lastText)
                        {
                            _lastText = currentText;
                            Log.Debug("[ClipboardWatcher] New text: {Text}", currentText);
                            OnClipboardTextChanged?.Invoke(currentText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[ClipboardWatcher] Error reading clipboard");
                    OnError?.Invoke(ex);
                }
                Thread.Sleep(_intervalMs);
            }
        }

        public void Stop()
        {
            _isRunning = false;
            if (_watcherThread != null && _watcherThread.IsAlive)
            {
                _watcherThread.Join();
                _watcherThread = null;
            }
            Log.Information("ClipboardWatcher stopped.");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
