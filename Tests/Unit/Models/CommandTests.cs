using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per Command model.
/// </summary>
public class CommandTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesCommand()
    {
        var command = new Command("Leggi variabile logica", 0x00, 0x01);

        Assert.Equal("Leggi variabile logica", command.Name);
        Assert.Equal(0x00, command.CodeHigh);
        Assert.Equal(0x01, command.CodeLow);
        Assert.False(command.IsResponse);
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void Constructor_WithParameters()
    {
        var parameters = new[] { "IndirizzoH", "IndirizzoL" };
        var command = new Command("Leggi variabile logica", 0x00, 0x01, false, parameters);

        Assert.Equal(2, command.Parameters.Count);
        Assert.Contains("IndirizzoH", command.Parameters);
        Assert.Contains("IndirizzoL", command.Parameters);
    }

    [Fact]
    public void Constructor_ResponseCommand()
    {
        var command = new Command("Leggi variabile logica risposta", 0x80, 0x01, isResponse: true);

        Assert.True(command.IsResponse);
        Assert.Equal(0x80, command.CodeHigh);
    }

    [Fact]
    public void FullCode_ReturnsCorrectValue()
    {
        var command = new Command("Test", 0x80, 0x07);

        Assert.Equal(0x8007, command.FullCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => new Command(name, 0x00, 0x01));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Command(null!, 0x00, 0x01));
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var parameters = new[] { "Param1", "Param2" };
        var command = Command.Restore(55, "Test", 0x00, 0x05, true, parameters);

        Assert.Equal(55, command.Id);
        Assert.Equal("Test", command.Name);
        Assert.True(command.IsResponse);
        Assert.Equal(2, command.Parameters.Count);
    }
}
