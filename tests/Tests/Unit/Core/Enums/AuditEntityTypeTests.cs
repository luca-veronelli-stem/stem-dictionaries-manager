using Core.Enums;

namespace Tests.Unit.Core.Enums;

/// <summary>
/// Test per AuditEntityType enum.
/// </summary>
public class AuditEntityTypeTests
{
    [Fact]
    public void AuditEntityType_HasExpectedCount()
    {
        AuditEntityType[] values = Enum.GetValues<AuditEntityType>();
        Assert.Equal(10, values.Length);
    }

    [Fact]
    public void AuditEntityType_ContainsAllEntityTypes()
    {
        Assert.True(Enum.IsDefined(AuditEntityType.Variable));
        Assert.True(Enum.IsDefined(AuditEntityType.Command));
        Assert.True(Enum.IsDefined(AuditEntityType.Board));
        Assert.True(Enum.IsDefined(AuditEntityType.Dictionary));
        Assert.True(Enum.IsDefined(AuditEntityType.BitInterpretation));
        Assert.True(Enum.IsDefined(AuditEntityType.User));
        Assert.True(Enum.IsDefined(AuditEntityType.Device));
        Assert.True(Enum.IsDefined(AuditEntityType.StandardVariableOverride));
        // Spec 001 — admin mint/revoke audit surfaces.
        Assert.True(Enum.IsDefined(AuditEntityType.BootstrapToken));
        Assert.True(Enum.IsDefined(AuditEntityType.Installation));
    }
}
