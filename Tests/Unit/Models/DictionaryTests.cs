using Core.Enums;
using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per Dictionary model.
/// </summary>
public class DictionaryTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesDictionary()
    {
        var dictionary = new Dictionary("optimus-xp");

        Assert.Equal("optimus-xp", dictionary.Name);
        Assert.Null(dictionary.BoardType);
        Assert.Null(dictionary.Description);
        Assert.Empty(dictionary.Variables);
    }

    [Fact]
    public void Constructor_WithBoardType()
    {
        var boardType = new BoardType("Madre", 17);
        var dictionary = new Dictionary("optimus-xp", DeviceType.OptimusXp, boardType, "Dizionario OPTIMUS XP");

        Assert.Equal(DeviceType.OptimusXp, dictionary.DeviceType);
        Assert.Equal(boardType, dictionary.BoardType);
        Assert.Equal("Dizionario OPTIMUS XP", dictionary.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() => new Dictionary(name));
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Dictionary(null!));
    }

    [Fact]
    public void AddVariable_AddsToList()
    {
        var dictionary = new Dictionary("test");
        var variable = new Variable("Test", 0x00, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t");

        dictionary.AddVariable(variable);

        Assert.Single(dictionary.Variables);
        Assert.Contains(variable, dictionary.Variables);
    }

    [Fact]
    public void AddVariable_NullVariable_ThrowsArgumentNullException()
    {
        var dictionary = new Dictionary("test");

        Assert.Throws<ArgumentNullException>(() => dictionary.AddVariable(null!));
    }

    [Fact]
    public void AddVariable_DuplicateAddress_ThrowsInvalidOperationException()
    {
        var dictionary = new Dictionary("test");
        var var1 = new Variable("Var1", 0x00, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t");
        var var2 = new Variable("Var2", 0x00, 0x01, DataTypeKind.UInt16, 
            AccessMode.ReadWrite, "uint16_t");

        dictionary.AddVariable(var1);

        var ex = Assert.Throws<InvalidOperationException>(() => dictionary.AddVariable(var2));
        Assert.Contains("0x0001", ex.Message);
    }

    [Fact]
    public void RemoveVariable_RemovesFromList()
    {
        var dictionary = new Dictionary("test");
        var variable = new Variable("Test", 0x00, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t");
        dictionary.AddVariable(variable);

        dictionary.RemoveVariable(variable);

        Assert.Empty(dictionary.Variables);
    }

    [Fact]
    public void HasUniqueAddresses_AllUnique_ReturnsTrue()
    {
        var dictionary = new Dictionary("test");
        dictionary.AddVariable(new Variable("Var1", 0x00, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t"));
        dictionary.AddVariable(new Variable("Var2", 0x00, 0x02, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t"));
        dictionary.AddVariable(new Variable("Var3", 0x80, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t"));

        Assert.True(dictionary.HasUniqueAddresses());
    }

    [Fact]
    public void HasUniqueAddresses_EmptyDictionary_ReturnsTrue()
    {
        var dictionary = new Dictionary("test");

        Assert.True(dictionary.HasUniqueAddresses());
    }

    [Fact]
    public void Restore_SetsIdAndVariables()
    {
        var boardType = new BoardType("Madre", 17);
        var variables = new List<Variable>
        {
            new("Var1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("Var2", 0x00, 0x02, DataTypeKind.UInt16, AccessMode.ReadWrite, "uint16_t")
        };

        var dictionary = Dictionary.Restore(10, "test", DeviceType.Optimus, boardType, "Description", variables);

        Assert.Equal(10, dictionary.Id);
        Assert.Equal("test", dictionary.Name);
        Assert.Equal(DeviceType.Optimus, dictionary.DeviceType);
        Assert.Equal(boardType, dictionary.BoardType);
        Assert.Equal(2, dictionary.Variables.Count);
    }

    [Fact]
    public void Constructor_Standard_BothNull_IsAccepted()
    {
        var dictionary = new Dictionary("standard");

        Assert.Null(dictionary.DeviceType);
        Assert.Null(dictionary.BoardType);
    }

    [Fact]
    public void Constructor_DeviceTypeWithoutBoardType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Dictionary("bad", DeviceType.Optimus, null));
    }

    [Fact]
    public void Constructor_BoardTypeWithoutDeviceType_ThrowsArgumentException()
    {
        var boardType = new BoardType("Madre", 17);

        Assert.Throws<ArgumentException>(() =>
            new Dictionary("bad", null, boardType));
    }

    [Fact]
    public void Restore_Standard_BothNull_IsAccepted()
    {
        var dictionary = Dictionary.Restore(1, "standard", null, null, "desc", []);

        Assert.Null(dictionary.DeviceType);
        Assert.Null(dictionary.BoardType);
    }
}
