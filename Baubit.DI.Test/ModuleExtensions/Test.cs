using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Baubit.DI.Test.ModuleExtensions
{
    /// <summary>
    /// Unit tests for <see cref="DI.ModuleExtensions"/>
    /// </summary>
    public class Test
    {
        #region Test Module Types

        /// <summary>
        /// Test configuration for unit tests.
        /// </summary>
        public class TestConfiguration : AConfiguration
        {
            public string? TestValue { get; set; }
            public int NumericValue { get; set; }
        }

        /// <summary>
        /// Test module for unit tests.
        /// </summary>
        public class TestModule : AModule<TestConfiguration>
        {
            public TestModule(TestConfiguration configuration, List<IModule>? nestedModules = null) 
                : base(configuration, nestedModules)
            {
            }

            public TestModule(IConfiguration configuration) : base(configuration)
            {
            }

            public override void Load(IServiceCollection services)
            {
                base.Load(services);
            }
        }

        #endregion

        #region Serialize Tests

        [Fact]
        public void Serialize_WithSimpleModule_ReturnsJsonString()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test123", NumericValue = 42 };
            var module = new TestModule(config);
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            var result = module.Serialize(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Contains("type", result.Value);
            Assert.Contains("configuration", result.Value);
            Assert.Contains("TestValue", result.Value);
            Assert.Contains("test123", result.Value);
        }

        [Fact]
        public void Serialize_WithIndentedOption_ReturnsIndentedJson()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "value" };
            var module = new TestModule(config);
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Act
            var result = module.Serialize(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("\n", result.Value); // Indented JSON contains newlines
        }

        [Fact]
        public void Serialize_WithNullOptions_ReturnsNonIndentedJson()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "value" };
            var module = new TestModule(config);

            // Act
            var result = module.Serialize(null!);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.DoesNotContain("\n", result.Value);
        }

        [Fact]
        public void Serialize_WithNestedModules_IncludesNestedModulesInOutput()
        {
            // Arrange
            var nestedConfig = new TestConfiguration { TestValue = "nested" };
            var nestedModule = new TestModule(nestedConfig);
            var parentConfig = new TestConfiguration { TestValue = "parent" };
            var parentModule = new TestModule(parentConfig, new List<IModule> { nestedModule });
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            var result = parentModule.Serialize(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("modules", result.Value);
            Assert.Contains("nested", result.Value);
            Assert.Contains("parent", result.Value);
        }

        [Fact]
        public void Serialize_WithEmptyNestedModules_IncludesEmptyModulesArray()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config, new List<IModule>());
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            var result = module.Serialize(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("\"modules\":[]", result.Value);
        }

        #endregion

        #region SerializeAsJsonObject Tests

        [Fact]
        public void SerializeAsJsonObject_WithEmptyCollection_ReturnsEmptyModulesArray()
        {
            // Arrange
            var modules = new List<IModule>();
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            var result = modules.SerializeAsJsonObject(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("{\"modules\":[]}", result.Value);
        }

        [Fact]
        public void SerializeAsJsonObject_WithSingleModule_ReturnsJsonWithModulesArray()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config);
            var modules = new List<IModule> { module };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            var result = modules.SerializeAsJsonObject(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("\"modules\":[", result.Value);
            Assert.Contains("test", result.Value);
        }

        [Fact]
        public void SerializeAsJsonObject_WithMultipleModules_ReturnsAllModules()
        {
            // Arrange
            var config1 = new TestConfiguration { TestValue = "first" };
            var config2 = new TestConfiguration { TestValue = "second" };
            var module1 = new TestModule(config1);
            var module2 = new TestModule(config2);
            var modules = new List<IModule> { module1, module2 };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            var result = modules.SerializeAsJsonObject(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("first", result.Value);
            Assert.Contains("second", result.Value);
        }

        [Fact]
        public void SerializeAsJsonObject_WithIndentedOption_ReturnsIndentedJson()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config);
            var modules = new List<IModule> { module };
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Act
            var result = modules.SerializeAsJsonObject(options);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("\n", result.Value);
        }

        [Fact]
        public void SerializeAsJsonObject_WithNullOptions_ReturnsNonIndentedJson()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config);
            var modules = new List<IModule> { module };

            // Act
            var result = modules.SerializeAsJsonObject(null!);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.DoesNotContain("\n", result.Value);
        }

        #endregion

        #region Round-trip Tests

        [Fact]
        public void Serialize_OutputCanBeDeserialized_PreservesModuleType()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "roundtrip", NumericValue = 100 };
            var module = new TestModule(config);
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act - Serialize
            var serializeResult = module.Serialize(options);
            Assert.True(serializeResult.IsSuccess);

            // Parse to verify structure
            using var doc = JsonDocument.Parse(serializeResult.Value);
            var root = doc.RootElement;

            // Assert - Structure is correct
            Assert.True(root.TryGetProperty("type", out var typeElement));
            Assert.Contains(typeof(TestModule).FullName!, typeElement.GetString());
            Assert.True(root.TryGetProperty("configuration", out var configElement));
            Assert.True(configElement.TryGetProperty("TestValue", out var testValueElement));
            Assert.Equal("roundtrip", testValueElement.GetString());
        }

        [Fact]
        public void SerializeAsJsonObject_OutputCanBeUsedToLoadModules()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "loadable" };
            var module = new TestModule(config);
            var modules = new List<IModule> { module };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act - Serialize
            var serializeResult = modules.SerializeAsJsonObject(options);
            Assert.True(serializeResult.IsSuccess);

            // Parse to verify structure
            using var doc = JsonDocument.Parse(serializeResult.Value);
            var root = doc.RootElement;

            // Assert - Can be used with ModuleBuilder.CreateMany
            Assert.True(root.TryGetProperty("modules", out var modulesElement));
            Assert.Equal(JsonValueKind.Array, modulesElement.ValueKind);
            Assert.Single(modulesElement.EnumerateArray());
        }

        #endregion

        #region TryFlatten Tests

        [Fact]
        public void TryFlatten_WithNoNestedModules_ReturnsListWithSingleModule()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config);

            // Act
            var result = module.TryFlatten();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            Assert.Same(module, result.Value[0]);
        }

        [Fact]
        public void TryFlatten_WithNestedModules_ReturnsFlattenedList()
        {
            // Arrange
            var nestedConfig1 = new TestConfiguration { TestValue = "nested1" };
            var nestedModule1 = new TestModule(nestedConfig1);
            var nestedConfig2 = new TestConfiguration { TestValue = "nested2" };
            var nestedModule2 = new TestModule(nestedConfig2);
            var parentConfig = new TestConfiguration { TestValue = "parent" };
            var parentModule = new TestModule(parentConfig, new List<IModule> { nestedModule1, nestedModule2 });

            // Act
            var result = parentModule.TryFlatten();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.Count);
            Assert.Same(parentModule, result.Value[0]);
            Assert.Same(nestedModule1, result.Value[1]);
            Assert.Same(nestedModule2, result.Value[2]);
        }

        [Fact]
        public void TryFlatten_WithDeeplyNestedModules_ReturnsAllModulesFlattened()
        {
            // Arrange
            var deepestConfig = new TestConfiguration { TestValue = "deepest" };
            var deepestModule = new TestModule(deepestConfig);
            var middleConfig = new TestConfiguration { TestValue = "middle" };
            var middleModule = new TestModule(middleConfig, new List<IModule> { deepestModule });
            var topConfig = new TestConfiguration { TestValue = "top" };
            var topModule = new TestModule(topConfig, new List<IModule> { middleModule });

            // Act
            var result = topModule.TryFlatten();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value.Count);
            Assert.Same(topModule, result.Value[0]);
            Assert.Same(middleModule, result.Value[1]);
            Assert.Same(deepestModule, result.Value[2]);
        }

        [Fact]
        public void TryFlatten_WithListParameter_AddsToExistingList()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config);
            var modules = new List<IModule>();

            // Act
            var result = module.TryFlatten(modules);

            // Assert
            Assert.True(result);
            Assert.Single(modules);
            Assert.Same(module, modules[0]);
        }

        [Fact]
        public void TryFlatten_WithNullListParameter_CreatesNewList()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config);

            // Act
            var result = module.TryFlatten(null!);

            // Assert
            Assert.True(result);
        }

        #endregion
    }
}
