#if WINDOWS
using System.Windows.Media;
using GUI.Windows.Converters;

namespace Tests.Unit.GUI.Converters;

/// <summary>
/// Test per BoolToErrorBrushConverter.
/// </summary>
public class BoolToErrorBrushConverterTests
{
    private readonly BoolToErrorBrushConverter _converter = new();

    [Fact]
    public void Convert_True_ReturnsErrorBrush()
    {
        var result = _converter.Convert(true, typeof(Brush), null!, null!) as SolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal("#FFF44336", result.Color.ToString());
    }

    [Fact]
    public void Convert_False_ReturnsNormalBrush()
    {
        var result = _converter.Convert(false, typeof(Brush), null!, null!) as SolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal("#FF555555", result.Color.ToString());
    }

    [Fact]
    public void Convert_NonBool_ReturnsNormalBrush()
    {
        var result = _converter.Convert("invalid", typeof(Brush), null!, null!) as SolidColorBrush;

        Assert.NotNull(result);
        Assert.Equal("#FF555555", result.Color.ToString());
    }
}
#endif
