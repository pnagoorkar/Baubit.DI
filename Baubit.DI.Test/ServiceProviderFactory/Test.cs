using Baubit.DI.Test.ServiceProviderFactory.Setup;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MsConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Baubit.DI.Test.ServiceProviderFactory
{
    /// <summary>
    /// Unit tests for <see cref="DI.ServiceProviderFactory"/>
    /// </summary>
    public class Test
    {
        [Theory]
        [InlineData("Baubit.DI.Test;ServiceProviderFactory.Setup.config.json")]
        public void Constructor_WithValidConfig_LoadsModulesSuccessfully(string configFile)
        {
            // Arrange & Act - Config references test-serviceprovider which IS in secure registry
            var result = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build())
                .Bind(cfg => Result.Try(() => Host.CreateApplicationBuilder().UseConfiguredServiceProviderFactory(cfg).Build()));

            // Assert - Should succeed because test modules ARE registered via TestModuleRegistry
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Constructor_WithServiceProviderOptions_CanBeCreated()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder().Build();

            // Act - Create factory directly without modules
            var factory = new Baubit.DI.ServiceProviderFactory(configuration, []);
            var services = new ServiceCollection();
            factory.Load(services);

            // Assert - With no modules, no services are added
            Assert.Empty(services);
        }

        [Fact]
        public void Load_WithNoModules_DoesNothing()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var factory = new Baubit.DI.ServiceProviderFactory(configuration, []);
            var services = new ServiceCollection();

            // Act
            factory.Load(services);

            // Assert
            Assert.Empty(services);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_ReturnsSuccessResult()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", typeof(TestModule).AssemblyQualifiedName }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var factory = new Baubit.DI.ServiceProviderFactory(configuration, []);
            var builder = Host.CreateApplicationBuilder();

            // Act
            var result = factory.UseConfiguredServiceProviderFactory(builder);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Same(builder, result.Value);
        }

        [Fact]
        public void CreateServiceProvider_WithNullServices_ReturnsWorkingServiceProvider()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder().Build();
            var factory = new Baubit.DI.ServiceProviderFactory(configuration, []);

            // Act - null default triggers the `services ?? new ServiceCollection()` branch
            var serviceProvider = factory.CreateServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider);
        }

        [Fact]
        public void CreateServiceProvider_WithProvidedServices_IncludesPreRegisteredServices()
        {
            // Arrange
            var configuration = new MsConfigurationBuilder().Build();
            var factory = new Baubit.DI.ServiceProviderFactory(configuration, []);
            var services = new ServiceCollection();
            services.AddSingleton<MyComponent>();

            // Act - non-null services takes the non-null branch of `services ?? new ServiceCollection()`
            var serviceProvider = factory.CreateServiceProvider(services);

            // Assert
            Assert.NotNull(serviceProvider);
            Assert.NotNull(serviceProvider.GetService<MyComponent>());
        }

        [Theory]
        [InlineData("Baubit.DI.Test;ServiceProviderFactory.Setup.config.json")]
        public void CreateServiceProvider_WithModules_CanResolveModuleServices(string configFile)
        {
            // Arrange
            var configuration = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build()).Value;

            var factory = new Baubit.DI.ServiceProviderFactory(configuration, []);

            // Act
            var serviceProvider = factory.CreateServiceProvider();

            // Assert - TestModule registers MyComponent, so it must be resolvable
            Assert.NotNull(serviceProvider);
            Assert.NotNull(serviceProvider.GetService<MyComponent>());
        }
    }
}
