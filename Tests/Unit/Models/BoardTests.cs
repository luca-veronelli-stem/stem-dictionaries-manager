using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per Board model.
/// Verifica il calcolo dell'indirizzo protocol.
/// </summary>
public class BoardTests
{
    private readonly BoardType _madreBoardType = new("Madre", 17);
    private readonly BoardType _pulsantieraBoardType = new("Pulsantiera", 4);

    [Fact]
    public void Constructor_ValidInput_CreatesBoard()
    {
        var board = new Board(DeviceType.OptimusXp, _madreBoardType, "Madre", 1, "DIS0020477");

        Assert.Equal(DeviceType.OptimusXp, board.DeviceType);
        Assert.Equal(_madreBoardType, board.BoardType);
        Assert.Equal("Madre", board.Name);
        Assert.Equal(1, board.BoardNumber);
        Assert.Equal("DIS0020477", board.PartNumber);
        Assert.Equal(0, board.Id);
    }

    [Fact]
    public void Constructor_NullPartNumber_IsValid()
    {
        var board = new Board(DeviceType.OptimusXp, _madreBoardType, "Madre", 1);

        Assert.Null(board.PartNumber);
    }

    [Fact]
    public void Constructor_NullBoardType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Board(DeviceType.OptimusXp, null!, "Madre", 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Board(DeviceType.OptimusXp, _madreBoardType, name, 1));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Board(DeviceType.OptimusXp, _madreBoardType, null!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(64)]
    [InlineData(100)]
    public void Constructor_InvalidBoardNumber_ThrowsArgumentOutOfRangeException(int boardNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Board(DeviceType.OptimusXp, _madreBoardType, "Madre", boardNumber));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(63)]
    public void Constructor_ValidBoardNumber_IsAccepted(int boardNumber)
    {
        var board = new Board(DeviceType.OptimusXp, _madreBoardType, "Test", boardNumber);

        Assert.Equal(boardNumber, board.BoardNumber);
    }

    // Test calcolo indirizzo protocol da indirizzi.csv
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
        var result = Board.CalculateAddress(machine, fwType, boardNum);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ProtocolAddress_ReturnsCalculatedValue()
    {
        var boardType = new BoardType("Madre", 17);
        var board = new Board(DeviceType.OptimusXp, boardType, "Madre", 1);

        // OPTIMUS-XP = 10, firmwareType = 17, boardNumber = 1 => 0x000A0441
        Assert.Equal(0x000A0441u, board.ProtocolAddress);
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var board = Board.Restore(99, DeviceType.EdenBs8, _madreBoardType, "Madre", 1, "DIS123", false);

        Assert.Equal(99, board.Id);
        Assert.Equal(DeviceType.EdenBs8, board.DeviceType);
        Assert.Equal("DIS123", board.PartNumber);
    }

    [Fact]
    public void Constructor_DefaultIsPrimary_IsFalse()
    {
        var board = new Board(DeviceType.OptimusXp, _madreBoardType, "Periferica", 2);

        Assert.False(board.IsPrimary);
    }

    [Fact]
    public void Constructor_IsPrimaryTrue_SetsProperty()
    {
        var board = new Board(DeviceType.OptimusXp, _madreBoardType, "Madre", 1, isPrimary: true);

        Assert.True(board.IsPrimary);
    }

    [Fact]
    public void Restore_IsPrimaryTrue_SetsProperty()
    {
        var board = Board.Restore(1, DeviceType.Spyke, _madreBoardType, "HMI", 1, null, true);

        Assert.True(board.IsPrimary);
    }
}
