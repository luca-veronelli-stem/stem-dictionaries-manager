#if WINDOWS
using GUI.Windows.ViewModels;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per CommandParameterItem (serializzazione, deserializzazione, display).
/// </summary>
public class CommandParameterItemTests
{
    [Fact]
    public void Serialize_FormatsAsPipeSeparated()
    {
        var item = new CommandParameterItem { Index = 0, SizeBytes = "2", Description = "Indirizzo" };
        Assert.Equal("2|Indirizzo", item.Serialize());
    }

    [Fact]
    public void Serialize_EmptyFields_ProducesPipe()
    {
        var item = new CommandParameterItem { Index = 0 };
        Assert.Equal("|", item.Serialize());
    }

    [Fact]
    public void Deserialize_StructuredFormat_ParsesCorrectly()
    {
        var item = CommandParameterItem.Deserialize(0, "4|Valore registro");
        Assert.Equal(0, item.Index);
        Assert.Equal("4", item.SizeBytes);
        Assert.Equal("Valore registro", item.Description);
    }

    [Fact]
    public void Deserialize_LegacyFormat_FallsBackToDescription()
    {
        var item = CommandParameterItem.Deserialize(2, "old_param");
        Assert.Equal(2, item.Index);
        Assert.Equal("", item.SizeBytes);
        Assert.Equal("old_param", item.Description);
    }

    [Fact]
    public void Deserialize_MultiplePipes_SplitsOnFirst()
    {
        var item = CommandParameterItem.Deserialize(0, "2|desc|with|pipes");
        Assert.Equal("2", item.SizeBytes);
        Assert.Equal("desc|with|pipes", item.Description);
    }

    [Fact]
    public void IndexDisplay_FormatsCorrectly()
    {
        var item = new CommandParameterItem { Index = 0 };
        Assert.Equal("Parametro 1", item.IndexDisplay);

        var item2 = new CommandParameterItem { Index = 4 };
        Assert.Equal("Parametro 5", item2.IndexDisplay);
    }

    [Fact]
    public void Roundtrip_SerializeDeserialize_PreservesData()
    {
        var original = new CommandParameterItem { Index = 1, SizeBytes = "2", Description = "Indirizzo" };
        var serialized = original.Serialize();
        var restored = CommandParameterItem.Deserialize(1, serialized);

        Assert.Equal(original.Index, restored.Index);
        Assert.Equal(original.SizeBytes, restored.SizeBytes);
        Assert.Equal(original.Description, restored.Description);
    }
}
#endif
