using GUI.Windows.Abstractions;

namespace GUI.Windows.Services;

/// <summary>
/// Implementazione di INavigationService.
/// Gestisce la navigazione tra view con history stack.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Stack<(ViewType View, NavigationParameter? Parameter)> _history = new();
    private ViewType _currentView = ViewType.DictionaryList;
    private NavigationParameter? _currentParameter;

    public ViewType CurrentView => _currentView;

    /// <summary>
    /// Parametro della navigazione corrente.
    /// </summary>
    public NavigationParameter? CurrentParameter => _currentParameter;

    public bool CanGoBack => _history.Count > 0;

    public event EventHandler<ViewType>? CurrentViewChanged;

    public void NavigateTo(ViewType viewType, NavigationParameter? parameter = null)
    {
        // Salva la view corrente nella history
        _history.Push((_currentView, _currentParameter));

        // Naviga alla nuova view
        _currentView = viewType;
        _currentParameter = parameter;

        OnCurrentViewChanged();
    }

    public bool GoBack()
    {
        if (!CanGoBack)
            return false;

        var (previousView, previousParameter) = _history.Pop();
        _currentView = previousView;
        _currentParameter = previousParameter;

        OnCurrentViewChanged();
        return true;
    }

    private void OnCurrentViewChanged()
    {
        CurrentViewChanged?.Invoke(this, _currentView);
    }
}
