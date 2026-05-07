using GUI.Windows.Abstractions;
using GUI.Windows.Views;

namespace GUI.Windows.Services;

/// <summary>
/// WPF implementation of IDialogService.
/// Uses the custom DarkDialog (dark theme).
/// </summary>
public sealed class DialogService : IDialogService
{
    public Task<DialogResult> ShowConfirmAsync(string title, string message)
    {
        bool result = DarkDialog.ShowConfirm(title, message);
        return Task.FromResult(result ? DialogResult.Yes : DialogResult.No);
    }

    public Task<DialogResult> ShowOkCancelAsync(string title, string message)
    {
        bool result = DarkDialog.ShowOkCancel(title, message);
        return Task.FromResult(result ? DialogResult.Ok : DialogResult.Cancel);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        DarkDialog.ShowError(title, message);
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        DarkDialog.ShowInfo(title, message);
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        DarkDialog.ShowWarning(title, message);
        return Task.CompletedTask;
    }
}
