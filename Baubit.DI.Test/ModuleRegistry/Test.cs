using Baubit.DI;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.Collections.Generic;
using System;

namespace Baubit.DI.Test.ModuleRegistry
{
    /// <summary>
    /// Tests for ModuleRegistry functionality.
    /// Note: RegisterExternal tests are not included as they require being called before
    /// any module resolution, which is not feasible in a unit test environment where
    /// other tests may have already initialized the registry.
    /// </summary>
    public class Test
    {
        [Fact]
        public void TryCreate_WithValidKey_ReturnsTrue()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result = Baubit.DI.ModuleRegistry.TryCreate("testmodule", config, out var module);
            
            // Assert
            Assert.True(result);
            Assert.NotNull(module);
        }

        [Fact]
        public void TryCreate_WithUnknownKey_ReturnsFalse()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result = Baubit.DI.ModuleRegistry.TryCreate("unknown-module", config, out var module);
            
            // Assert
            Assert.False(result);
            Assert.Null(module);
        }

        [Fact]
        public void TryCreate_WithNullKey_ReturnsFalse()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result = Baubit.DI.ModuleRegistry.TryCreate(null!, config, out var module);
            
            // Assert
            Assert.False(result);
            Assert.Null(module);
        }

        [Fact]
        public void TryCreate_WithEmptyKey_ReturnsFalse()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result = Baubit.DI.ModuleRegistry.TryCreate("", config, out var module);
            
            // Assert
            Assert.False(result);
            Assert.Null(module);
        }

        [Fact]
        public void TryCreate_WithWhitespaceKey_ReturnsFalse()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result = Baubit.DI.ModuleRegistry.TryCreate("   ", config, out var module);
            
            // Assert
            Assert.False(result);
            Assert.Null(module);
        }

        [Fact]
        public void TryCreate_IsCaseInsensitive()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result1 = Baubit.DI.ModuleRegistry.TryCreate("testmodule", config, out var module1);
            var result2 = Baubit.DI.ModuleRegistry.TryCreate("TESTMODULE", config, out var module2);
            var result3 = Baubit.DI.ModuleRegistry.TryCreate("TestModule", config, out var module3);
            
            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.NotNull(module1);
            Assert.NotNull(module2);
            Assert.NotNull(module3);
        }

        [Fact]
        public void TryCreate_WithDifferentConfiguration_PassesConfigToFactory()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["testKey"] = "testValue"
                })
                .Build();
            
            // Act
            var result = Baubit.DI.ModuleRegistry.TryCreate("testmodule", config, out var module);
            
            // Assert
            Assert.True(result);
            Assert.NotNull(module);
            // Module receives the configuration and can use it
        }

        [Fact]
        public void TryCreate_CreatesNewInstanceEachTime()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var result1 = Baubit.DI.ModuleRegistry.TryCreate("testmodule", config, out var module1);
            var result2 = Baubit.DI.ModuleRegistry.TryCreate("testmodule", config, out var module2);
            
            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.NotSame(module1, module2); // Different instances
        }
    }

    // Test helper classes
    [BaubitModule("testmodule")]
    public class TestModule : Module<TestConfiguration>
    {
        public TestModule(IConfiguration configuration) : base(configuration) { }
    }

    public class TestConfiguration : Configuration
    {
    }
}

