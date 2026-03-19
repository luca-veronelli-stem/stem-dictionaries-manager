#if WINDOWS
using System.Globalization;
using System.Windows.Data;
using GUI.Windows.Converters;

namespace Tests.Unit.GUI.Converters;

/// <summary>
/// Test per NullableIntConverter e NullableDoubleConverter.
/// </summary>
public class NullableNumericConverterTests
{
    #region NullableIntConverter Tests

    public class NullableIntConverterTests
    {
        private readonly NullableIntConverter _converter = new();

        [Fact]
        public void Convert_NullValue_ReturnsEmptyString()
        {
            var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Convert_IntValue_ReturnsStringRepresentation()
        {
            var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal("42", result);
        }

        [Fact]
        public void Convert_ZeroValue_ReturnsZeroString()
        {
            var result = _converter.Convert(0, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal("0", result);
        }

        [Fact]
        public void ConvertBack_EmptyString_ReturnsNull()
        {
            var result = _converter.ConvertBack("", typeof(int?), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void ConvertBack_WhitespaceString_ReturnsNull()
        {
            var result = _converter.ConvertBack("   ", typeof(int?), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void ConvertBack_ValidIntString_ReturnsInt()
        {
            var result = _converter.ConvertBack("42", typeof(int?), null, CultureInfo.InvariantCulture);
            Assert.Equal(42, result);
        }

        [Fact]
        public void ConvertBack_InvalidString_ReturnsDoNothing()
        {
            var result = _converter.ConvertBack("abc", typeof(int?), null, CultureInfo.InvariantCulture);
            Assert.Equal(Binding.DoNothing, result);
        }

        [Fact]
        public void ConvertBack_DecimalString_ReturnsDoNothing()
        {
            var result = _converter.ConvertBack("3.14", typeof(int?), null, CultureInfo.InvariantCulture);
            Assert.Equal(Binding.DoNothing, result);
        }

        [Fact]
        public void ConvertBack_NegativeInt_ReturnsNegativeInt()
        {
            var result = _converter.ConvertBack("-5", typeof(int?), null, CultureInfo.InvariantCulture);
            Assert.Equal(-5, result);
        }
    }

    #endregion

    #region NullableDoubleConverter Tests

    public class NullableDoubleConverterTests
    {
        private readonly NullableDoubleConverter _converter = new();

        [Fact]
        public void Convert_NullValue_ReturnsEmptyString()
        {
            var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Convert_DoubleValue_ReturnsStringRepresentation()
        {
            var result = _converter.Convert(3.14, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal("3.14", result);
        }

        [Fact]
        public void Convert_IntegerDoubleValue_ReturnsStringWithoutDecimals()
        {
            var result = _converter.Convert(42.0, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal("42", result);
        }

        [Fact]
        public void ConvertBack_EmptyString_ReturnsNull()
        {
            var result = _converter.ConvertBack("", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void ConvertBack_WhitespaceString_ReturnsNull()
        {
            var result = _converter.ConvertBack("   ", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void ConvertBack_ValidDoubleWithDot_ReturnsDouble()
        {
            var result = _converter.ConvertBack("3.14", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Equal(3.14, result);
        }

        [Fact]
        public void ConvertBack_ValidDoubleWithComma_ReturnsDouble()
        {
            // Il converter dovrebbe supportare sia '.' che ','
            var result = _converter.ConvertBack("3,14", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Equal(3.14, result);
        }

        [Fact]
        public void ConvertBack_IntegerString_ReturnsDouble()
        {
            var result = _converter.ConvertBack("42", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Equal(42.0, result);
        }

        [Fact]
        public void ConvertBack_NegativeDouble_ReturnsNegativeDouble()
        {
            var result = _converter.ConvertBack("-3.14", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Equal(-3.14, result);
        }

        [Fact]
        public void ConvertBack_InvalidString_ReturnsDoNothing()
        {
            var result = _converter.ConvertBack("abc", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Equal(Binding.DoNothing, result);
        }

        [Fact]
        public void ConvertBack_PartiallyInvalidString_ReturnsDoNothing()
        {
            var result = _converter.ConvertBack("3.14abc", typeof(double?), null, CultureInfo.InvariantCulture);
            Assert.Equal(Binding.DoNothing, result);
        }
    }

    #endregion
}
#endif
