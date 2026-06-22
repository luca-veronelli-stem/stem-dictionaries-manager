using GUI.Windows.Abstractions;
using Microsoft.Extensions.Logging;

namespace GUI.Windows.Services;

/// <summary>
/// INavigationService implementation.
/// Manages navigation between views with a history stack.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Stack<(ViewType View, NavigationParameter? Parameter, object? ViewModel)> _history = new();
    private readonly ILogger<NavigationService> _logger;
    private ViewType _currentView = ViewType.DeviceList;
    private NavigationParameter? _currentParameter;
    private object? _currentViewModel;

    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger;
    }

    public ViewType CurrentView => _currentView;

    /// <summary>
    /// Current navigation parameter.
    /// </summary>
    public NavigationParameter? CurrentParameter => _currentParameter;

    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// ViewModel restored by GoBack (null on forward navigation).
    /// </summary>
    public object? CachedViewModel { get; private set; }

    public event EventHandler<ViewType>? CurrentViewChanged;

    /// <summary>
    /// Registers the current ViewModel for caching in the history.
    /// </summary>
    public void SetCurrentViewModel(object? viewModel)
    {
        _currentViewModel = viewModel;
    }

    public void NavigateTo(ViewType viewType, NavigationParameter? parameter = null)
    {
        // Save the current view in the history (with the cached ViewModel)
        _history.Push((_currentView, _currentParameter, _currentViewModel));

        // Navigate to the new view (forward = no cache)
        _currentView = viewType;
        _currentParameter = parameter;
        _currentViewModel = null;
        CachedViewModel = null;

        _logger.LogDebug("Navigating forward to {ViewType}", viewType);
        OnCurrentViewChanged();
    }

    public bool GoBack()
    {
        if (!CanGoBack)
        {
            return false;
        }

        (ViewType previousView, NavigationParameter? previousParameter, object? cachedVm) = _history.Pop();
        _currentView = previousView;
        _currentParameter = previousParameter;
        CachedViewModel = cachedVm;
        _currentViewModel = cachedVm;

        _logger.LogDebug("Navigating back to {ViewType}", _currentView);
        OnCurrentViewChanged();
        return true;
    }

    /// <summary>
    /// Resets the navigation state to the initial view (DeviceList).
    /// Clears history and cached ViewModel.
    /// </summary>
    public void Reset()
    {
        _history.Clear();
        _currentView = ViewType.DeviceList;
        _currentParameter = null;
        _currentViewModel = null;
        CachedViewModel = null;
    }

    private void OnCurrentViewChanged()
    {
        CurrentViewChanged?.Invoke(this, _currentView);
    }
}
