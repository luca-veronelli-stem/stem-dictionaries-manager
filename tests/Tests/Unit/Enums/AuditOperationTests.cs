using Core.Enums;

namespace Tests.Unit.Enums;

/// <summary>
/// Test per AuditOperation enum.
/// </summary>
public class AuditOperationTests
{
    [Fact]
    public void AuditOperation_HasExpectedCount()
    {
        AuditOperation[] values = Enum.GetValues<AuditOperation>();
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void AuditOperation_ContainsAllCrudOperations()
    {
        Assert.True(Enum.IsDefined(AuditOperation.Create));
        Assert.True(Enum.IsDefined(AuditOperation.Update));
        Assert.True(Enum.IsDefined(AuditOperation.Delete));
    }
}
