using Core.Enums;

namespace Tests.Unit.Enums;

/// <summary>
/// Test per AuditEntityType enum.
/// </summary>
public class AuditEntityTypeTests
{
    [Fact]
    public void AuditEntityType_HasExpectedCount()
    {
        var values = Enum.GetValues<AuditEntityType>();
        Assert.Equal(6, values.Length);
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
    }
}
