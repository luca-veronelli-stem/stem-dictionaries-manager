#if WINDOWS
using GUI.Windows.Abstractions;

namespace Tests.Unit.GUI.Mocks;

/// <summary>
/// Mock implementation di INavigationService per i test.
/// </summary>
public class MockNavigationService : INavigationService
{
    private readonly Stack<ViewType> _history = new();
    
    public ViewType CurrentView { get; private set; } = ViewType.DictionaryList;
    public bool CanGoBack => _history.Count > 0;
    
    public NavigationParameter? LastParameter { get; private set; }
    public List<(ViewType View, NavigationParameter? Param)> NavigationHistory { get; } = [];
    
    public event EventHandler<ViewType>? CurrentViewChanged;

    public void NavigateTo(ViewType viewType, NavigationParameter? parameter = null)
    {
        _history.Push(CurrentView);
        CurrentView = viewType;
        LastParameter = parameter;
        NavigationHistory.Add((viewType, parameter));
        CurrentViewChanged?.Invoke(this, viewType);
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        CurrentView = _history.Pop();
        CurrentViewChanged?.Invoke(this, CurrentView);
        return true;
    }
    
    /// <summary>
    /// Reset per test isolation.
    /// </summary>
    public void Reset()
    {
        _history.Clear();
        CurrentView = ViewType.DictionaryList;
        LastParameter = null;
        NavigationHistory.Clear();
    }
}

/// <summary>
/// Mock implementation di IDialogService per i test.
/// </summary>
public class MockDialogService : IDialogService
{
    /// <summary>
    /// Risultato predefinito per ShowConfirmAsync.
    /// </summary>
    public DialogResult ConfirmResult { get; set; } = DialogResult.Yes;
    
    /// <summary>
    /// Risultato predefinito per ShowOkCancelAsync.
    /// </summary>
    public DialogResult OkCancelResult { get; set; } = DialogResult.Ok;
    
    /// <summary>
    /// Traccia le chiamate ai dialog.
    /// </summary>
    public List<(string Title, string Message, string Type)> Calls { get; } = [];

    public Task<DialogResult> ShowConfirmAsync(string title, string message)
    {
        Calls.Add((title, message, "Confirm"));
        return Task.FromResult(ConfirmResult);
    }

    public Task<DialogResult> ShowOkCancelAsync(string title, string message)
    {
        Calls.Add((title, message, "OkCancel"));
        return Task.FromResult(OkCancelResult);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        Calls.Add((title, message, "Error"));
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        Calls.Add((title, message, "Info"));
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        Calls.Add((title, message, "Warning"));
        return Task.CompletedTask;
    }
    
    public void Reset()
    {
        Calls.Clear();
        ConfirmResult = DialogResult.Yes;
        OkCancelResult = DialogResult.Ok;
    }
}

/// <summary>
/// Mock implementation di IMessageService per i test.
/// </summary>
public class MockMessageService : IMessageService
{
    public string? CurrentMessage { get; private set; }
    public MessageSeverity CurrentSeverity { get; private set; } = MessageSeverity.Info;
    
    public List<(string Message, MessageSeverity Severity)> Messages { get; } = [];
    
    public event EventHandler? MessageChanged;

    public void Show(string message, MessageSeverity severity = MessageSeverity.Info, int autoHideSeconds = 5)
    {
        CurrentMessage = message;
        CurrentSeverity = severity;
        Messages.Add((message, severity));
        MessageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        CurrentMessage = null;
        MessageChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void Reset()
    {
        CurrentMessage = null;
        CurrentSeverity = MessageSeverity.Info;
        Messages.Clear();
    }
}
#endif
