#if WINDOWS
using System.Windows.Media;
using GUI.Windows.Abstractions;
using GUI.Windows.Converters;

namespace Tests.Unit.GUI.Converters;

/// <summary>
/// Test per SeverityToColorConverter.
/// </summary>
public class SeverityToColorConverterTests
{
    private readonly SeverityToColorConverter _converter = new();

    [Theory]
    [InlineData(MessageSeverity.Info, "#FF3C3C3C")]
    [InlineData(MessageSeverity.Success, "#FF2E7D32")]
    [InlineData(MessageSeverity.Warning, "#FFE65C00")]
    [InlineData(MessageSeverity.Error, "#FFC62828")]
    public void Convert_Severity_ReturnsExpectedColor(MessageSeverity severity, string expectedColor)
    {
        var result = _converter.Convert(severity, typeof(Brush), null!, null!) as SolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal(expectedColor, result.Color.ToString());
    }

    [Fact]
    public void Convert_InvalidValue_ReturnsInfoBrush()
    {
        var result = _converter.Convert("invalid", typeof(Brush), null!, null!) as SolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal("#FF3C3C3C", result.Color.ToString());
    }
}
#endif
