using System.Windows;
using System.Windows.Threading;
using GUI.Windows.Abstractions;
using GUI.Windows.Views;

namespace GUI.Windows.Services;

/// <summary>
/// WPF implementation of IDialogService using the custom DarkDialog (dark
/// theme). Each call queues the modal dialog on the UI dispatcher and completes
/// a TaskCompletionSource when it closes, so the async methods return a pending
/// task without blocking the caller -- instead of running a synchronous
/// ShowDialog() and wrapping its result in Task.FromResult.
/// </summary>
public sealed class DialogService : IDialogService
{
    public Task<DialogResult> ShowConfirmAsync(string title, string message) =>
        InvokeAsync(() => DarkDialog.ShowConfirm(title, message) ? DialogResult.Yes : DialogResult.No);

    public Task<DialogResult> ShowOkCancelAsync(string title, string message) =>
        InvokeAsync(() => DarkDialog.ShowOkCancel(title, message) ? DialogResult.Ok : DialogResult.Cancel);

    public Task ShowErrorAsync(string title, string message) =>
        InvokeAsync(() => DarkDialog.ShowError(title, message));

    public Task ShowInfoAsync(string title, string message) =>
        InvokeAsync(() => DarkDialog.ShowInfo(title, message));

    public Task ShowWarningAsync(string title, string message) =>
        InvokeAsync(() => DarkDialog.ShowWarning(title, message));

    /// <summary>
    /// Queues <paramref name="showDialog"/> on the UI dispatcher and returns a
    /// task that completes with its result when the modal dialog closes. The
    /// call returns immediately with a pending task; it never shows the window
    /// inline, so the caller's frame and the message pump are not blocked.
    /// </summary>
    private static Task<T> InvokeAsync<T>(Func<T> showDialog)
    {
        Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        TaskCompletionSource<T> tcs = new();
        dispatcher.BeginInvoke(() =>
        {
            try
            {
                tcs.SetResult(showDialog());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    private static Task InvokeAsync(Action showDialog) =>
        InvokeAsync(() =>
        {
            showDialog();
            return true;
        });
}
