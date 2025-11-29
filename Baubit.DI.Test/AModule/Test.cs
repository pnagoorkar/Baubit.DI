using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Baubit.DI.Test.AModule
{
    /// <summary>
    /// Unit tests for <see cref="DI.AModule"/> and <see cref="DI.AModule{TConfiguration}"/>
    /// </summary>
    public class Test
    {
        #region Test Types

        public class TestConfiguration : AConfiguration
        {
            public string? TestValue { get; set; }
        }

        public class TestModule : AModule<TestConfiguration>
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

        public class TestModuleWithDependencies : AModule<TestConfiguration>
        {
            private readonly TestModule _dependency;

            public TestModuleWithDependencies(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
            {
                _dependency = new TestModule(new TestConfiguration(), new List<IModule>());
            }

            protected override IEnumerable<Baubit.DI.AModule> GetKnownDependencies()
            {
                return new[] { _dependency };
            }
        }

        #endregion

        #region AModule Constructor Tests

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
                { "type", typeof(TestModule).AssemblyQualifiedName },
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

        #region AModule Configuration Property Tests

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

        #region AModule Load Tests

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

        #endregion

        #region AModule NestedModules Tests

        [Fact]
        public void NestedModules_IsReadOnly()
        {
            // Arrange
            var config = new TestConfiguration();
            var module = new TestModule(config, new List<IModule>());

            // Act & Assert
            Assert.IsAssignableFrom<IReadOnlyList<IModule>>(module.NestedModules);
        }

        [Fact]
        public void NestedModules_WithNestedModulesInConfiguration_LoadsFromConfiguration()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "TestValue", "parent" },
                { "modules:0:type", typeof(TestModule).AssemblyQualifiedName },
                { "modules:0:TestValue", "nested1" }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var module = new TestModule(configuration);

            // Assert
            Assert.Single(module.NestedModules);
            Assert.IsType<TestModule>(module.NestedModules[0]);
        }

        #endregion
    }
}
