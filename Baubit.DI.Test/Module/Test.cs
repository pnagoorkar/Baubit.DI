using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Baubit.DI.Test.Module
{
    /// <summary>
    /// Unit tests for <see cref="DI.Module"/> and <see cref="DI.Module{TConfiguration}"/>
    /// </summary>
    public class Test
    {
        #region Test Types

        public class TestConfiguration : Configuration
        {
            public string? TestValue { get; set; }
        }

        [BaubitModule("test-basemodule")]
        public class TestModule : Module<TestConfiguration>
        {
            public bool LoadCalled { get; private set; }
            public bool OnInitializedCalled { get; private set; }

            public TestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
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
        /// Test module that uses the default null parameter for nestedModules.
        /// </summary>
        [BaubitModule("test-basemodule-null")]
        public class TestModuleWithNullDefault : Module<TestConfiguration>
        {
            public TestModuleWithNullDefault(TestConfiguration configuration) : base(configuration)
            {
            }

            public TestModuleWithNullDefault(IConfiguration configuration) : base(configuration)
            {
            }

            public override void Load(IServiceCollection services)
            {
            }
        }

        [BaubitModule("test-basemodule-deps")]
        public class TestModuleWithDependencies : Module<TestConfiguration>
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

            protected override IEnumerable<Baubit.DI.Module> GetKnownDependencies()
            {
                return new[] { _dependency };
            }
        }

        #endregion

        #region Module Constructor Tests

        [Fact]
        public void Constructor_WithConfigurationAndNestedModules_SetsProperties()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "test" };
            var nestedModules = new List<IModule>();

            // Act
            var module = new TestModule(config, nestedModules);

            // Assert
            Assert.Same(config, module.Configuration);
            Assert.NotNull(module.NestedModules);
            Assert.Empty(module.NestedModules);
        }

        [Fact]
        public void Constructor_WithIConfiguration_LoadsNestedModules()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "key", "test-basemodule" },
                { "TestValue", "testValue" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
            Assert.Equal("testValue", module.Configuration.TestValue);
            Assert.NotNull(module.NestedModules);
        }

        [Fact]
        public void Constructor_CallsOnInitialized()
        {
            // Arrange
            var config = new TestConfiguration();
            var nestedModules = new List<IModule>();

            // Act
            var module = new TestModule(config, nestedModules);

            // Assert
            Assert.True(module.OnInitializedCalled);
        }

        [Fact]
        public void Constructor_WithNullNestedModules_CreatesEmptyList()
        {
            // Arrange
            var config = new TestConfiguration();

            // Act - Using constructor that has nestedModules defaulting to null
            var module = new TestModuleWithNullDefault(config);

            // Assert
            Assert.NotNull(module.NestedModules);
            Assert.Empty(module.NestedModules);
        }

        [Fact]
        public void Constructor_CombinesNestedModulesWithKnownDependencies()
        {
            // Arrange
            var config = new TestConfiguration();
            var existingModule = new TestModule(new TestConfiguration(), new List<IModule>());
            var nestedModules = new List<IModule> { existingModule };

            // Act
            var module = new TestModuleWithDependencies(config, nestedModules);

            // Assert
            Assert.Equal(2, module.NestedModules.Count);
            Assert.Contains(existingModule, module.NestedModules);
        }

        #endregion

        #region Module Configuration Property Tests

        [Fact]
        public void Configuration_ReturnsStronglyTypedConfiguration()
        {
            // Arrange
            var config = new TestConfiguration { TestValue = "myValue" };
            var module = new TestModule(config, new List<IModule>());

            // Act
            var retrievedConfig = module.Configuration;

            // Assert
            Assert.IsType<TestConfiguration>(retrievedConfig);
            Assert.Equal("myValue", retrievedConfig.TestValue);
        }

        #endregion

        #region Module Load Tests

        [Fact]
        public void Load_WhenOverridden_IsCalled()
        {
            // Arrange
            var config = new TestConfiguration();
            var module = new TestModule(config, new List<IModule>());
            var services = new ServiceCollection();

            // Act
            module.Load(services);

            // Assert
            Assert.True(module.LoadCalled);
        }

        [Fact]
        public void Load_DefaultImplementation_DoesNothing()
        {
            // Arrange
            var config = new TestConfiguration();
            var module = new TestModule(config, new List<IModule>());
            var services = new ServiceCollection();
            var initialCount = services.Count;

            // Act
            module.Load(services);

            // Assert - services count should be unchanged by default base.Load
            Assert.Equal(initialCount, services.Count);
        }

        /// <summary>
        /// Helper module that tracks load order
        /// </summary>
        private class TrackingModule : Module<TestConfiguration>
        {
            private readonly string _name;
            private readonly List<string> _loadOrder;

            public TrackingModule(string name, List<string> loadOrder)
                : base(new TestConfiguration(), new List<IModule>())
            {
                _name = name;
                _loadOrder = loadOrder;
            }

            public override void Load(IServiceCollection services)
            {
                _loadOrder.Add(_name);
                base.Load(services);
            }
        }

        #endregion

        #region Module Constructor - Null Configuration Guard Tests (Issue #15)

        /// <summary>
        /// Regression test for issue #15:
        /// When IConfiguration has no keys at all, Configuration must not be null.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyIConfiguration_ConfigurationIsNotNull()
        {
            // Arrange – completely empty IConfiguration (no keys at all)
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
        }

        /// <summary>
        /// Regression test for issue #15:
        /// IConfiguration built from empty JSON ("{}") must not produce a null Configuration.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyJsonIConfiguration_ConfigurationIsNotNull()
        {
            // Arrange – IConfiguration built from an empty JSON string
            var configuration = new MsConfigurationBuilder()
                .AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}")))
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
        }

        /// <summary>
        /// Regression test for issue #15:
        /// When IConfiguration contains only the module routing key but no configuration property
        /// values, Configuration must not be null and its properties must have their default values.
        /// </summary>
        [Fact]
        public void Constructor_WithIConfigurationContainingOnlyModuleKey_ConfigurationIsNotNullAndHasDefaults()
        {
            // Arrange – only the routing key is present; no TestValue key
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "key", "test-basemodule" }
                })
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
            Assert.Null(module.Configuration.TestValue);   // default value for string is null
        }

        /// <summary>
        /// Regression test for issue #15:
        /// When IConfiguration has no properties that map to TConfiguration,
        /// Configuration must still be a valid (non-null) instance with all defaults.
        /// </summary>
        [Fact]
        public void Constructor_WithIConfigurationContainingIrrelevantKeys_ConfigurationIsNotNull()
        {
            // Arrange – keys that do not map to any property of TestConfiguration
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "unrelated:key", "some-value" }
                })
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
        }

        /// <summary>
        /// Verifies that when IConfiguration contains a value for a configuration property,
        /// the property is correctly bound even after the null-guard fix.
        /// </summary>
        [Fact]
        public void Constructor_WithIConfigurationContainingConfigValues_ConfigurationHasCorrectValues()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "TestValue", "expectedValue" }
                })
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
            Assert.Equal("expectedValue", module.Configuration.TestValue);
        }

        /// <summary>
        /// Verifies that the null-guard does not interfere when IConfiguration contains
        /// both the routing key and actual configuration values.
        /// </summary>
        [Fact]
        public void Constructor_WithIConfigurationContainingModuleKeyAndConfigValues_ConfigurationHasCorrectValues()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "key", "test-basemodule" },
                    { "TestValue", "hello" }
                })
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.NotNull(module.Configuration);
            Assert.Equal("hello", module.Configuration.TestValue);
        }

        /// <summary>
        /// Verifies that two modules constructed from the same empty IConfiguration both
        /// receive independent (non-shared) default configuration instances.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyIConfiguration_EachModuleGetsIndependentDefaultConfiguration()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            // Act
            var moduleA = new TestModule(configuration);
            var moduleB = new TestModule(configuration);

            // Assert – configurations are not the same reference
            Assert.NotSame(moduleA.Configuration, moduleB.Configuration);
        }

        #endregion

        #region Module NestedModules Tests

        [Fact]
        public void NestedModules_IsReadOnly()
        {
            // Arrange
            var config = new TestConfiguration();
            var module = new TestModule(config, new List<IModule>());

            // Act & Assert
            Assert.IsAssignableFrom<IReadOnlyList<IModule>>(module.NestedModules);
        }

        [Theory]
        [InlineData("Baubit.DI.Test;Module.Setup.config.json")]
        public void NestedModules_WithNestedModulesInConfiguration_LoadsFromConfiguration(string configFile)
        {
            // Act
            var moduleBuildResult = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                                                                             .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                                                                             .Bind(cb => cb.Build())
                                                                             .Bind(cfg => Result.Try(() => new TestModule(cfg)));

            Assert.True(moduleBuildResult.IsSuccess);

            var module = moduleBuildResult.Value;

            // Assert
            Assert.Single(module.NestedModules);
            Assert.IsType<TestModule>(module.NestedModules[0]);
        }

        #endregion
    }
}
