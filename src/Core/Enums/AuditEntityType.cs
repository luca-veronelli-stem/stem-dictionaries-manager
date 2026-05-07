namespace Core.Enums;

/// <summary>
/// Tipo di entità modificata, per l'audit trail.
/// v7: +StandardVariableOverride, -VariableDeviceState.
/// </summary>
public enum AuditEntityType
{
    Variable,
    Command,
    Board,
    Dictionary,
    BitInterpretation,
    User,
    Device,
    StandardVariableOverride
}
