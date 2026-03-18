using System.Windows;
using GUI.Windows.Abstractions;

namespace GUI.Windows.Services;

/// <summary>
/// Implementazione WPF di IDialogService.
/// Usa MessageBox standard di Windows.
/// </summary>
public sealed class DialogService : IDialogService
{
    public Task<DialogResult> ShowConfirmAsync(string title, string message)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return Task.FromResult(result == MessageBoxResult.Yes
            ? DialogResult.Yes
            : DialogResult.No);
    }

    public Task<DialogResult> ShowOkCancelAsync(string title, string message)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        return Task.FromResult(result == MessageBoxResult.OK
            ? DialogResult.Ok
            : DialogResult.Cancel);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task ShowInfoAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return Task.CompletedTask;
    }
}
