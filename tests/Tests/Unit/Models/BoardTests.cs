using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per Board model (Domain v2).
/// FirmwareType diretto, DictionaryId opzionale, nessun BoardType.
/// </summary>
public class BoardTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesBoard()
    {
        var board = new Board(10, "Madre", 17, 1, 10, "DIS0020477");

        Assert.Equal(10, board.DeviceId);
        Assert.Equal("Madre", board.Name);
        Assert.Equal(17, board.FirmwareType);
        Assert.Equal(1, board.BoardNumber);
        Assert.Equal("DIS0020477", board.PartNumber);
        Assert.Equal(0, board.Id);
        Assert.Null(board.DictionaryId);
    }

    [Fact]
    public void Constructor_NullPartNumber_IsValid()
    {
        var board = new Board(10, "Madre", 17, 1, 10);
        Assert.Null(board.PartNumber);
    }

    [Fact]
    public void Constructor_WithDictionaryId_SetsProperty()
    {
        var board = new Board(10, "Madre", 17, 1, 10, dictionaryId: 42);
        Assert.Equal(42, board.DictionaryId);
    }

    [Fact]
    public void Constructor_NullDictionaryId_IsValid()
    {
        var board = new Board(7, "Motore DX", 21, 2, 7);
        Assert.Null(board.DictionaryId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Board(10, name, 17, 1, 10));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Board(10, null!, 17, 1, 10));
    }

    [Fact]
    public void Constructor_NegativeFirmwareType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Board(10, "Madre", -1, 1, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(64)]
    [InlineData(100)]
    public void Constructor_InvalidBoardNumber_ThrowsArgumentOutOfRangeException(int boardNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Board(10, "Madre", 17, boardNumber, 10));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(63)]
    public void Constructor_ValidBoardNumber_IsAccepted(int boardNumber)
    {
        var board = new Board(10, "Test", 17, boardNumber, 10);
        Assert.Equal(boardNumber, board.BoardNumber);
    }

    [Theory]
    [InlineData(10, 17, 1, 0x000A0441)]  // OPTIMUS-XP Madre
    [InlineData(10, 4, 1, 0x000A0101)]   // OPTIMUS-XP Tastiera1
    [InlineData(10, 4, 2, 0x000A0102)]   // OPTIMUS-XP Tastiera2
    [InlineData(10, 4, 3, 0x000A0103)]   // OPTIMUS-XP Tastiera3
    [InlineData(12, 19, 1, 0x000C04C1)]  // EDEN BS8 Madre
    [InlineData(11, 18, 1, 0x000B0481)]  // R3L-XP Madre Master
    [InlineData(11, 20, 1, 0x000B0501)]  // R3L-XP Madre Slave
    [InlineData(1, 1, 1, 0x00010041)]    // SHERPA SLIM Azionamento
    public void CalculateAddress_ReturnsCorrectValue(int machine, int fwType, int boardNum, uint expected)
    {
        Assert.Equal(expected, Board.CalculateAddress(machine, fwType, boardNum));
    }

    [Fact]
    public void ProtocolAddress_ReturnsCalculatedValue()
    {
        var board = new Board(10, "Madre", 17, 1, machineCode: 10);
        Assert.Equal(0x000A0441u, board.ProtocolAddress);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var board = Board.Restore(99, 12, "Madre", 19, 1, "DIS123", false, 5, machineCode: 12);

        Assert.Equal(99, board.Id);
        Assert.Equal(12, board.DeviceId);
        Assert.Equal(19, board.FirmwareType);
        Assert.Equal("DIS123", board.PartNumber);
        Assert.Equal(5, board.DictionaryId);
    }

    [Fact]
    public void Restore_WithDictionaryName_SetsProperty()
    {
        var board = Board.Restore(1, 10, "Madre", 17, 1, null, false, 5, machineCode: 10, dictionaryName: "Standard");

        Assert.Equal("Standard", board.DictionaryName);
        Assert.Equal(5, board.DictionaryId);
    }

    [Fact]
    public void Restore_WithoutDictionaryName_DefaultsToNull()
    {
        var board = Board.Restore(1, 10, "Madre", 17, 1, null, false, null, machineCode: 10);

        Assert.Null(board.DictionaryName);
    }

    [Fact]
    public void Constructor_DictionaryName_IsNull()
    {
        _ = new Board(10, "Madre", 17, 1, 10);
    }

    [Fact]
    public void Constructor_DefaultIsPrimary_IsFalse()
    {
        var board = new Board(10, "Periferica", 4, 2, 10);
        Assert.False(board.IsPrimary);
    }

    [Fact]
    public void Constructor_IsPrimaryTrue_SetsProperty()
    {
        var board = new Board(10, "Madre", 17, 1, 10, isPrimary: true);
        Assert.True(board.IsPrimary);
    }

    [Fact]
    public void Restore_IsPrimaryTrue_SetsProperty()
    {
        var board = Board.Restore(1, 5, "HMI", 20, 1, null, true, null, machineCode: 5);
        Assert.True(board.IsPrimary);
    }
}
