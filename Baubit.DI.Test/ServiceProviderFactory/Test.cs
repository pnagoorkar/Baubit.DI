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
    }
}
