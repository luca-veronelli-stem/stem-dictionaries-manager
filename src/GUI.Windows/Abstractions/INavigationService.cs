namespace GUI.Windows.Abstractions;

/// <summary>
/// View-type identifiers for navigation.
/// </summary>
public enum ViewType
{
    DeviceList,
    DeviceDetail,
    DeviceEdit,
    DeviceCommands,
    DictionaryList,
    DictionaryEdit,
    VariableEdit,
    CommandList,
    CommandEdit,
    BoardEdit,
    UserList,
    Settings
}

/// <summary>
/// Navigation parameters for passing data between views.
/// </summary>
public record NavigationParameter
{
    /// <summary>
    /// ID of the entity to edit (null = new entity).
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Parent ID (e.g., DictionaryId for VariableList).
    /// </summary>
    public int? ParentId { get; init; }

    /// <summary>
    /// Device ID used as a filter (e.g., DeviceDetail).
    /// </summary>
    public int? DeviceId { get; init; }

    /// <summary>
    /// Additional parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Extra { get; init; }
}

/// <summary>
/// Navigation service between views.
/// Manages history and parameter passing.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Currently displayed view.
    /// </summary>
    ViewType CurrentView { get; }

    /// <summary>
    /// Current navigation parameter.
    /// </summary>
    NavigationParameter? CurrentParameter { get; }

    /// <summary>
    /// Indicates whether backward navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// ViewModel restored by GoBack (null on forward navigation).
    /// </summary>
    object? CachedViewModel { get; }

    /// <summary>
    /// Registers the current ViewModel for caching in the history.
    /// </summary>
    void SetCurrentViewModel(object? viewModel);

    /// <summary>
    /// Navigates to a specific view.
    /// </summary>
    void NavigateTo(ViewType viewType, NavigationParameter? parameter = null);

    /// <summary>
    /// Navigates to the previous view.
    /// </summary>
    /// <returns>True if navigation occurred, false if there is no history.</returns>
    bool GoBack();

    /// <summary>
    /// Resets the navigation state to the initial view.
    /// Clears history and cached ViewModel.
    /// </summary>
    void Reset();

    /// <summary>
    /// Event raised when the current view changes.
    /// </summary>
    event EventHandler<ViewType>? CurrentViewChanged;
}
