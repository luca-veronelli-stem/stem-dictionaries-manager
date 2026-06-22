using Core.Enums;
using Core.Models;

namespace Tests.Unit.Core.Models;

/// <summary>
/// Test per Dictionary model (Domain v2).
/// IsStandard flag, nessun deviceId/BoardType.
/// </summary>
public class DictionaryTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesDictionary()
    {
        var dictionary = new Dictionary("optimus-xp");

        Assert.Equal("optimus-xp", dictionary.Name);
        Assert.Null(dictionary.Description);
        Assert.False(dictionary.IsStandard);
        Assert.Empty(dictionary.Variables);
    }

    [Fact]
    public void Constructor_WithDescription_SetsProperty()
    {
        var dictionary = new Dictionary("optimus-xp", "Dizionario OPTIMUS XP");

        Assert.Equal("Dizionario OPTIMUS XP", dictionary.Description);
    }

    [Fact]
    public void Constructor_IsStandard_SetsProperty()
    {
        var dictionary = new Dictionary("Standard", isStandard: true);

        Assert.True(dictionary.IsStandard);
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

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => dictionary.AddVariable(var2));
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
    public void RemoveVariable_AbsentVariable_ThrowsInvalidOperationException()
    {
        var dictionary = new Dictionary("test");
        var absent = new Variable("Absent", 0x00, 0x01, DataTypeKind.UInt8,
            AccessMode.ReadOnly, "uint8_t");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => dictionary.RemoveVariable(absent));
        Assert.Contains("Absent", ex.Message);
    }

    [Fact]
    public void RemoveVariable_NullVariable_ThrowsArgumentNullException()
    {
        var dictionary = new Dictionary("test");
        Assert.Throws<ArgumentNullException>(() => dictionary.RemoveVariable(null!));
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
        var variables = new List<Variable>
        {
            new("Var1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("Var2", 0x00, 0x02, DataTypeKind.UInt16, AccessMode.ReadWrite, "uint16_t")
        };

        var dictionary = Dictionary.Restore(10, "test", "Description", false, variables);

        Assert.Equal(10, dictionary.Id);
        Assert.Equal("test", dictionary.Name);
        Assert.Equal("Description", dictionary.Description);
        Assert.False(dictionary.IsStandard);
        Assert.Equal(2, dictionary.Variables.Count);
    }

    [Fact]
    public void Restore_Standard_SetsIsStandard()
    {
        var dictionary = Dictionary.Restore(1, "Standard", "desc", true, []);

        Assert.True(dictionary.IsStandard);
    }

    [Fact]
    public void Restore_DuplicateAddress_ThrowsInvalidOperationException()
    {
        var variables = new List<Variable>
        {
            new("Var1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("Var2", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadWrite, "uint16_t") // stesso indirizzo
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Dictionary.Restore(10, "test", "Description", false, variables));

        Assert.Contains("0x0001", ex.Message);
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public void Restore_MultipleDuplicates_ListsAllInMessage()
    {
        var variables = new List<Variable>
        {
            new("Var1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("Var2", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"), // dup 0x0001
            new("Var3", 0x00, 0x02, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("Var4", 0x80, 0x10, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("Var5", 0x80, 0x10, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t")  // dup 0x8010
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Dictionary.Restore(10, "test", null, false, variables));

        Assert.Contains("0x0001", ex.Message);
        Assert.Contains("0x8010", ex.Message);
    }

    [Fact]
    public void Restore_SameAddressLowDifferentHigh_Succeeds()
    {
        // 0x0001 (Standard) e 0x8001 (DeviceSpecific) sono indirizzi DIVERSI
        var variables = new List<Variable>
        {
            new("StandardVar", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new("DeviceVar", 0x80, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t")
        };

        var dictionary = Dictionary.Restore(10, "test", null, false, variables);

        Assert.Equal(2, dictionary.Variables.Count);
    }

    [Fact]
    public void UpdateName_ValidValue_UpdatesName()
    {
        var dictionary = new Dictionary("old-name");

        dictionary.UpdateName("new-name");

        Assert.Equal("new-name", dictionary.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_InvalidValue_ThrowsArgumentException(string name)
    {
        var dictionary = new Dictionary("old-name");

        Assert.Throws<ArgumentException>(() => dictionary.UpdateName(name));
    }

    [Fact]
    public void UpdateDescription_SetsValue()
    {
        var dictionary = new Dictionary("test");

        dictionary.UpdateDescription("A description");

        Assert.Equal("A description", dictionary.Description);
    }

    [Fact]
    public void UpdateDescription_Null_ClearsValue()
    {
        var dictionary = new Dictionary("test", "Initial");

        dictionary.UpdateDescription(null);

        Assert.Null(dictionary.Description);
    }
}
