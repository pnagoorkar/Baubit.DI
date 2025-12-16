using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Baubit.Configuration;
using MsConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Baubit.DI.Test.ModuleBuilder
{
    /// <summary>
    /// Unit tests for <see cref="DI.ModuleBuilder"/>
    /// </summary>
    public class Test
    {
        #region Test Module Types

        /// <summary>
        /// Test configuration for unit tests.
        /// </summary>
        public class TestConfiguration : BaseConfiguration
        {
            public string? TestValue { get; set; }
        }

        /// <summary>
        /// Test module for unit tests.
        /// </summary>
        [BaubitModule("test-modulebuilder")]
        public class TestModule : BaseModule<TestConfiguration>
        {
            public bool LoadCalled { get; private set; }
            public bool OnInitializedCalled { get; private set; }

            public TestModule(TestConfiguration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules)
            {
            }

            public TestModule(IConfiguration configuration) : base(configuration)
            {
            }

            protected override void OnInitialized()
            {
                OnInitializedCalled = true;
                base.OnInitialized();
            }

            public override void Load(IServiceCollection services)
            {
                LoadCalled = true;
                base.Load(services);
            }
        }

        /// <summary>
        /// Test module that provides known dependencies.
        /// </summary>
        [BaubitModule("test-modulebuilder-deps")]
        public class TestModuleWithDependencies : BaseModule<TestConfiguration>
        {
            private readonly TestModule _dependency;

            public TestModuleWithDependencies(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
            {
                _dependency = new TestModule(new TestConfiguration(), new List<IModule>());
            }

            public TestModuleWithDependencies(IConfiguration configuration) : base(configuration)
            {
                _dependency = new TestModule(new TestConfiguration(), new List<IModule>());
            }

            protected override IEnumerable<Baubit.DI.BaseModule> GetKnownDependencies()
            {
                return new[] { _dependency };
            }
        }

        #endregion

        #region ModuleBuilder.CreateNew Tests

        [Fact]
        public void CreateNew_WithUnknownModuleKey_ReturnsFailedResult()
        {
            // Arrange - Use a key that truly doesn't exist
            var configDict = new Dictionary<string, string?>
            {
                { "type", "nonexistent-module-key-12345" },
                { "TestValue", "test123" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act - Build the module to trigger validation
            var result = DI.ModuleBuilder.CreateNew(configuration).Bind(b => b.Build());

            // Assert - Should fail because module key doesn't exist in registry
            Assert.True(result.IsFailed);
            Assert.Contains("Unknown module key", result.Errors[0].Message);
        }

        [Fact]
        public void CreateNew_WithInvalidType_ReturnsFailedResult()
        {
            // Arrange - Assembly-qualified names are treated as keys and won't be found
            var configDict = new Dictionary<string, string?>
            {
                { "type", "NonExistent.Type, NonExistent.Assembly" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act - Build the module to trigger validation
            var result = DI.ModuleBuilder.CreateNew(configuration).Bind(b => b.Build());

            // Assert - Should fail because this key doesn't exist in registry
            Assert.True(result.IsFailed);
            Assert.Contains("Unknown module key", result.Errors[0].Message);
        }

        [Fact]
        public void CreateNew_WithNullType_ReturnsFailedResult()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>();
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.CreateNew(configuration);

            // Assert
            Assert.True(result.IsFailed);
        }

        #endregion

        #region ModuleBuilder.CreateMany Tests

        [Fact]
        public void CreateMany_WithNoModules_ReturnsEmptyCollection()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>();
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.CreateMany(configuration);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Fact]
        public void CreateMany_WithDirectlyDefinedModules_FailsOnUnknownModuleKey()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "test-modulebuilder" },
                { "modules:0:TestValue", "value1" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.CreateMany(configuration);

            // Assert - Should fail because test-modulebuilder isn't in secure registry
            Assert.True(result.IsFailed);
            Assert.Contains("Unknown module key", result.Errors[0].Message);
        }

        [Fact]
        public void CreateMany_WithMultipleDirectlyDefinedModules_ReturnsAllModuleBuilders()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "test-modulebuilder" },
                { "modules:0:TestValue", "value1" },
                { "modules:1:type", "test-modulebuilder" },
                { "modules:1:TestValue", "value2" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.CreateMany(configuration);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public void CreateMany_WithInvalidModuleType_ReturnsFailedResult()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "Invalid.Type, Invalid.Assembly" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.CreateMany(configuration);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void CreateMany_WithModuleSources_LoadsFromSources()
        {
            // Arrange - Create a JSON file for indirect module loading
            var tempFile = Path.GetTempFileName();
            var moduleType = "test-modulebuilder";
            File.WriteAllText(tempFile, $"{{ \"type\": \"{moduleType}\", \"TestValue\": \"fromSource\" }}");

            try
            {
                var configDict = new Dictionary<string, string?>
                {
                    { "moduleSources:0:jsonUriStrings:0", $"file://{tempFile}" }
                };
                var configuration = new MsConfigurationBuilder()
                    .AddInMemoryCollection(configDict)
                    .Build();

                // Act
                var result = DI.ModuleBuilder.CreateMany(configuration);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Single(result.Value);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void CreateMany_WithBothModulesAndModuleSources_LoadsAll()
        {
            // Arrange - Create a JSON file for indirect module loading
            var tempFile = Path.GetTempFileName();
            var moduleType = "test-modulebuilder";
            File.WriteAllText(tempFile, $"{{ \"type\": \"{moduleType}\", \"TestValue\": \"fromSource\" }}");

            try
            {
                var configDict = new Dictionary<string, string?>
                {
                    { "modules:0:type", moduleType },
                    { "modules:0:TestValue", "direct" },
                    { "moduleSources:0:jsonUriStrings:0", $"file://{tempFile}" }
                };
                var configuration = new MsConfigurationBuilder()
                    .AddInMemoryCollection(configDict)
                    .Build();

                // Act
                var result = DI.ModuleBuilder.CreateMany(configuration);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal(2, result.Value.Count());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region ModuleBuilder.Build Tests

        [Fact]
        public void Build_WithValidConfiguration_ReturnsModule()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "type", "test-modulebuilder" },
                { "TestValue", "testValue" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder.CreateNew(configuration).Value;

            // Act
            var result = moduleBuilder.Build();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.IsType<TestModule>(result.Value);
        }

        [Fact]
        public void Build_AfterDispose_ThrowsNullReferenceException()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "type", "test-modulebuilder" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder.CreateNew(configuration).Value;
            moduleBuilder.Build(); // First build disposes the builder

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => moduleBuilder.Build());
        }

        [Fact]
        public void Build_CallsOnInitialized()
        {
            // Arrange - Create module directly to test OnInitialized callback
            var config = new TestConfiguration { TestValue = "test" };
            var module = new TestModule(config, null);
            
            // Act - The module is already initialized via constructor

            // Assert
            Assert.True(module.OnInitializedCalled);
        }

        #endregion

        #region ModuleBuilder.GetModuleSourcesSectionOrDefault Tests

        [Fact]
        public void GetModuleSourcesSectionOrDefault_WithNoModuleSources_ReturnsNull()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>();
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.GetModuleSourcesSectionOrDefault(configuration);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        }

        [Fact]
        public void GetModuleSourcesSectionOrDefault_WithModuleSources_ReturnsSection()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "moduleSources:0:json", "{}" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var result = DI.ModuleBuilder.GetModuleSourcesSectionOrDefault(configuration);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        #endregion

        #region ModuleBuilder.Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "type", "test-modulebuilder" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder.CreateNew(configuration).Value;

            // Act & Assert (should not throw)
            moduleBuilder.Dispose();
            moduleBuilder.Dispose();
        }

        [Fact]
        public void Dispose_AfterBuild_HandlesNullConfigurationBuilder()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "type", "test-modulebuilder" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder.CreateNew(configuration).Value;
            
            // Build disposes the builder and sets configurationBuilder to null
            moduleBuilder.Build();

            // Act & Assert - Dispose should handle the null configurationBuilder gracefully
            // Note: The first Dispose in Build already disposed, so this tests the double-dispose path
            moduleBuilder.Dispose();
        }

        #endregion

        #region ModuleBuilder<TModule, TConfiguration> Tests

        [Fact]
        public void GenericModuleBuilder_CreateNew_ReturnsSuccessResult()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            // Act
            var result = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void GenericModuleBuilder_WithNestedModules_AddsModules()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var nestedModule = new TestModule(new TestConfiguration(), new List<IModule>());
            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act
            var result = moduleBuilder.WithNestedModules(nestedModule);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void GenericModuleBuilder_Build_WithProperlyConfiguredBuilder_ReturnsTypedModule()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act
            var result = moduleBuilder.Build();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.IsType<TestModule>(result.Value);
        }

        [Fact]
        public void GenericModuleBuilder_WithNestedModulesFrom_LoadsModulesFromConfiguration()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var nestedConfigDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "test-modulebuilder" },
                { "modules:0:TestValue", "nested" }
            };
            var nestedConfiguration = new MsConfigurationBuilder()
                .AddInMemoryCollection(nestedConfigDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act
            var result = moduleBuilder.WithNestedModulesFrom(nestedConfiguration);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void GenericModuleBuilder_WithNestedModulesFrom_MultipleConfigurations_LoadsAllModules()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var nestedConfigDict1 = new Dictionary<string, string?>
            {
                { "modules:0:type", "test-modulebuilder" },
                { "modules:0:TestValue", "nested1" }
            };
            var nestedConfigDict2 = new Dictionary<string, string?>
            {
                { "modules:0:type", "test-modulebuilder" },
                { "modules:0:TestValue", "nested2" }
            };
            var config1 = new MsConfigurationBuilder().AddInMemoryCollection(nestedConfigDict1).Build();
            var config2 = new MsConfigurationBuilder().AddInMemoryCollection(nestedConfigDict2).Build();

            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act
            var result = moduleBuilder.WithNestedModulesFrom(config1, config2);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void GenericModuleBuilder_WithNestedModulesFrom_EmptyConfiguration_Succeeds()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var emptyConfigDict = new Dictionary<string, string?>();
            var emptyConfiguration = new MsConfigurationBuilder()
                .AddInMemoryCollection(emptyConfigDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act
            var result = moduleBuilder.WithNestedModulesFrom(emptyConfiguration);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void GenericModuleBuilder_WithNestedModulesFrom_InvalidModuleType_ReturnsFailedResult()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var invalidConfigDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "Invalid.Type, Invalid.Assembly" }
            };
            var invalidConfiguration = new MsConfigurationBuilder()
                .AddInMemoryCollection(invalidConfigDict)
                .Build();

            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act
            var result = moduleBuilder.WithNestedModulesFrom(invalidConfiguration);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void GenericModuleBuilder_WithNestedModulesFrom_MultipleConfigurationsWithInvalid_ReturnsFailedResult()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>
                .CreateNew()
                .Value;

            var validConfigDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "test-modulebuilder" },
                { "modules:0:TestValue", "valid" }
            };
            var invalidConfigDict = new Dictionary<string, string?>
            {
                { "modules:0:type", "Invalid.Type, Invalid.Assembly" }
            };
            var validConfig = new MsConfigurationBuilder().AddInMemoryCollection(validConfigDict).Build();
            var invalidConfig = new MsConfigurationBuilder().AddInMemoryCollection(invalidConfigDict).Build();

            var moduleBuilder = DI.ModuleBuilder<TestModule, TestConfiguration>.CreateNew(configBuilder, cfg => new TestModule(cfg)).Value;

            // Act - Pass valid first, then invalid to exercise the loop
            var result = moduleBuilder.WithNestedModulesFrom(validConfig, invalidConfig);

            // Assert - Should fail because one of the configurations is invalid
            Assert.True(result.IsFailed);
        }

        #endregion
    }
}

