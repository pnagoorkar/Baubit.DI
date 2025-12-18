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
                { "type", "test-basemodule" },
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
