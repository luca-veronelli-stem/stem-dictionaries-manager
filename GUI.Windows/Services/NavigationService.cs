using GUI.Windows.Abstractions;

namespace GUI.Windows.Services;

/// <summary>
/// Implementazione di INavigationService.
/// Gestisce la navigazione tra view con history stack.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Stack<(ViewType View, NavigationParameter? Parameter, object? ViewModel)> _history = new();
    private ViewType _currentView = ViewType.DictionaryList;
    private NavigationParameter? _currentParameter;
    private object? _currentViewModel;

    public ViewType CurrentView => _currentView;

    /// <summary>
    /// Parametro della navigazione corrente.
    /// </summary>
    public NavigationParameter? CurrentParameter => _currentParameter;

    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// ViewModel ripristinato dal GoBack (null se navigazione forward).
    /// </summary>
    public object? CachedViewModel { get; private set; }

    public event EventHandler<ViewType>? CurrentViewChanged;

    /// <summary>
    /// Registra il ViewModel corrente per il caching nella history.
    /// </summary>
    public void SetCurrentViewModel(object? viewModel)
    {
        _currentViewModel = viewModel;
    }

    public void NavigateTo(ViewType viewType, NavigationParameter? parameter = null)
    {
        // Salva la view corrente nella history (con il ViewModel cached)
        _history.Push((_currentView, _currentParameter, _currentViewModel));

        // Naviga alla nuova view (forward = no cache)
        _currentView = viewType;
        _currentParameter = parameter;
        _currentViewModel = null;
        CachedViewModel = null;

        OnCurrentViewChanged();
    }

    public bool GoBack()
    {
        if (!CanGoBack)
            return false;

        var (previousView, previousParameter, cachedVm) = _history.Pop();
        _currentView = previousView;
        _currentParameter = previousParameter;
        CachedViewModel = cachedVm;
        _currentViewModel = cachedVm;

        OnCurrentViewChanged();
        return true;
    }

    private void OnCurrentViewChanged()
    {
        CurrentViewChanged?.Invoke(this, _currentView);
    }
}
