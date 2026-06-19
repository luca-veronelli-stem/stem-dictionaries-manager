#if WINDOWS
using GUI.Windows.Abstractions;
using GUI.Windows.Services;

namespace Tests.Unit.GUI.Services;

/// <summary>
/// Tests for DialogService (#16): the async methods must marshal the modal
/// dialog onto the UI dispatcher through a TaskCompletionSource and return
/// without blocking, instead of wrapping a synchronous DarkDialog.ShowDialog()
/// in Task.FromResult (which built and showed the window before returning).
/// </summary>
public class DialogServiceTests
{
    [Fact]
    public void ShowConfirmAsync_DefersDialogToDispatcher_ReturnsPendingTask()
    {
        DialogService service = new();

        Task<DialogResult> task = service.ShowConfirmAsync("Title", "Message");

        // The modal dialog is queued on the UI dispatcher, so the call returns a
        // pending task immediately rather than constructing/showing the WPF
        // window inline. With no dispatcher loop pumping (as in a headless test)
        // the dialog never opens and the task simply stays pending.
        Assert.False(task.IsCompleted);
    }

    [Fact]
    public void ShowOkCancelAsync_DefersDialogToDispatcher_ReturnsPendingTask()
    {
        DialogService service = new();

        Task<DialogResult> task = service.ShowOkCancelAsync("Title", "Message");

        Assert.False(task.IsCompleted);
    }

    [Fact]
    public void ShowErrorAsync_DefersDialogToDispatcher_ReturnsPendingTask()
    {
        DialogService service = new();

        Task task = service.ShowErrorAsync("Title", "Message");

        Assert.False(task.IsCompleted);
    }
}
#endif
