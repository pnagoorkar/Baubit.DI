using System;

namespace Baubit.DI.Test
{
    /// <summary>
    /// Unit tests for <see cref="BaubitModuleAttribute"/>.
    /// </summary>
    public class BaubitModuleAttributeTests
    {
        [Fact]
        public void Constructor_WithValidKey_SetsKey()
        {
            // Act
            var attribute = new BaubitModuleAttribute("my-module");

            // Assert
            Assert.Equal("my-module", attribute.Key);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmptyOrWhitespaceKey_ThrowsArgumentException(string key)
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new BaubitModuleAttribute(key));
            Assert.Equal("key", ex.ParamName);
        }
    }
}
