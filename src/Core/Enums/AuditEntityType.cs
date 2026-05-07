namespace Core.Enums;

/// <summary>
/// Type of entity modified, for the audit trail.
/// v7: +StandardVariableOverride, -VariableDeviceState.
/// v8 (spec 001): +BootstrapToken, +Installation appended (admin mint/revoke).
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
    StandardVariableOverride,
    BootstrapToken,
    Installation
}
