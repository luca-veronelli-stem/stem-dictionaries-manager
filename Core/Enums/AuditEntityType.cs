namespace Core.Enums;

/// <summary>
/// Tipo di entità modificata, per l'audit trail.
/// SESSION_024: rimosso BoardType (entità eliminata dal dominio).
/// SESSION_035: aggiunto Device (DeviceType enum → Device entity).
/// </summary>
public enum AuditEntityType
{
    Variable,
    Command,
    Board,
    Dictionary,
    BitInterpretation,
    User,
    Device
}
