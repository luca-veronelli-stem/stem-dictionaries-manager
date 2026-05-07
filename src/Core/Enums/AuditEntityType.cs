namespace Core.Enums;

/// <summary>
/// Type of entity modified, for the audit trail.
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
