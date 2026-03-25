namespace GUI.Windows.Abstractions;

/// <summary>
/// Interfaccia per ViewModel con stato modificato (form di edit).
/// Usata da MainViewModel per warning su navigazione indietro con modifiche non salvate.
/// </summary>
public interface IEditableViewModel
{
    /// <summary>
    /// True se il form ha modifiche non salvate.
    /// </summary>
    bool HasChanges { get; }
}
