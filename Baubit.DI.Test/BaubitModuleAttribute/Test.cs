namespace Baubit.DI.Test.BaubitModuleAttribute
{
    /// <summary>
    /// Unit tests for <see cref="BaubitModuleAttribute"/>.
    /// </summary>
    public class Test
    {
        [Fact]
        public void Constructor_WithValidKey_SetsKey()
        {
            // Act
            var attribute = new Baubit.DI.BaubitModuleAttribute("my-module");

            // Assert
            Assert.Equal("my-module", attribute.Key);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyOrWhitespaceKey_ThrowsArgumentException(string key)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new Baubit.DI.BaubitModuleAttribute(key));
            Assert.Equal("key", ex.ParamName);
        }
    }
}
