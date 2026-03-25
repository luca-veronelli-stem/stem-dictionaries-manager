namespace Core.Enums;

/// <summary>
/// Tipo di entità modificata, per l'audit trail.
/// SESSION_024: rimosso BoardType (entità eliminata dal dominio).
/// </summary>
public enum AuditEntityType
{
    Variable,
    Command,
    Board,
    Dictionary,
    BitInterpretation,
    User
}
