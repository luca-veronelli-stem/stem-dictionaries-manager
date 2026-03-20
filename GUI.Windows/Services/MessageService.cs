using GUI.Windows.Abstractions;
using System.Timers;
using Timer = System.Timers.Timer;

namespace GUI.Windows.Services;

/// <summary>
/// Implementazione di IMessageService per status bar.
/// Thread-safe con auto-hide timer.
/// </summary>
public sealed class MessageService : IMessageService, IDisposable
{
    private readonly Lock _lock = new();
    private readonly Timer _hideTimer;
    private string? _currentMessage;
    private MessageSeverity _currentSeverity;

    public MessageService()
    {
        _hideTimer = new Timer
        {
            AutoReset = false
        };
        _hideTimer.Elapsed += OnTimerElapsed;
    }

    public string? CurrentMessage
    {
        get { lock (_lock) return _currentMessage; }
        private set { lock (_lock) _currentMessage = value; }
    }

    public MessageSeverity CurrentSeverity
    {
        get { lock (_lock) return _currentSeverity; }
        private set { lock (_lock) _currentSeverity = value; }
    }

    public event EventHandler? MessageChanged;

    public void Show(string message, MessageSeverity severity = MessageSeverity.Info, int autoHideSeconds = 5)
    {
        _hideTimer.Stop();

        CurrentMessage = message;
        CurrentSeverity = severity;

        if (autoHideSeconds > 0)
        {
            _hideTimer.Interval = autoHideSeconds * 1000;
            _hideTimer.Start();
        }

        OnMessageChanged();
    }

    public void Clear()
    {
        _hideTimer.Stop();
        CurrentMessage = null;
        CurrentSeverity = MessageSeverity.Info;
        OnMessageChanged();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        Clear();
    }

    private void OnMessageChanged()
    {
        MessageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _hideTimer.Stop();
        _hideTimer.Dispose();
    }
}
