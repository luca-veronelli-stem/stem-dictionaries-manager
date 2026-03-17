using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per BoardType model.
/// </summary>
public class BoardTypeTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesBoardType()
    {
        var boardType = new BoardType("Madre", 17);

        Assert.Equal("Madre", boardType.Name);
        Assert.Equal(17, boardType.FirmwareType);
        Assert.Equal(0, boardType.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => new BoardType(name, 17));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BoardType(null!, 17));
    }

    [Fact]
    public void Constructor_NegativeFirmwareType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoardType("Madre", -1));
    }

    [Fact]
    public void Constructor_ZeroFirmwareType_IsValid()
    {
        var boardType = new BoardType("Test", 0);

        Assert.Equal(0, boardType.FirmwareType);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var boardType = BoardType.Restore(5, "Pulsantiera", 4);

        Assert.Equal(5, boardType.Id);
        Assert.Equal("Pulsantiera", boardType.Name);
        Assert.Equal(4, boardType.FirmwareType);
    }
}
