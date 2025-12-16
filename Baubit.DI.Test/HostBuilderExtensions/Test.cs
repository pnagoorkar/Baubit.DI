using Baubit.DI.Test.HostBuilderExtensions.Setup;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MsConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Baubit.DI.Test.HostBuilderExtensions
{
    /// <summary>
    /// Unit tests for <see cref="DI.HostBuilderExtensions"/>
    /// </summary>
    public class Test
    {
        [Theory]
        [InlineData("Baubit.DI.Test;HostBuilderExtensions.Setup.config.json")]
        public void UseConfiguredServiceProviderFactory_WithValidConfig_FailsOnUnknownModuleKey(string configFile)
        {
            // Arrange & Act - Config references test-hostbuilder which isn't in secure registry
            var result = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build())
                .Bind(cfg => Result.Try(() => Host.CreateApplicationBuilder().UseConfiguredServiceProviderFactory(cfg).Build()));

            // Assert - Should fail because test modules aren't in the secure ModuleRegistry
            Assert.True(result.IsFailed);
            Assert.Contains("Unknown module key", result.Errors[0].Message);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_WithCustomFactoryTypeParameter_UsesCustomFactory()
        {
            // Arrange
            CustomServiceProviderFactory.Reset();
            var configuration = new ConfigurationBuilder().Build();

            // Act - Pass custom factory type via generic parameter
            var builder = Host.CreateApplicationBuilder();
            builder.UseConfiguredServiceProviderFactory<HostApplicationBuilder, CustomServiceProviderFactory>(configuration);
            var result = Result.Try(() => builder.Build());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(CustomServiceProviderFactory.WasCreated);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_WithoutFactoryTypeParameter_UsesDefaultFactory()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();

            // Act - No factory type parameter provided, should use default
            var builder = Host.CreateApplicationBuilder();
            builder.UseConfiguredServiceProviderFactory(configuration);
            var result = Result.Try(() => builder.Build());

            // Assert - Should succeed with default factory
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_WithNoConfiguration_UsesDefaultFactory()
        {
            // Arrange & Act
            var builder = Host.CreateApplicationBuilder();
            var result = builder.UseConfiguredServiceProviderFactory();

            // Assert - Should not throw and return the same builder
            Assert.Same(builder, result);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_WithNullOnFailure_UsesDefaultHandler()
        {
            // Arrange
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", typeof(TestModule).AssemblyQualifiedName }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var builder = Host.CreateApplicationBuilder();
            var result = builder.UseConfiguredServiceProviderFactory(configuration, null);

            // Assert - Should not throw and return the builder
            Assert.Same(builder, result);
        }

        [Theory]
        [InlineData("Baubit.DI.Test;HostBuilderExtensions.Setup.config.json")]
        public void UseConfiguredServiceProviderFactory_WithGenericFactoryType_UsesSpecifiedFactory(string configFile)
        {
            // Arrange
            CustomServiceProviderFactory.Reset();

            // Act
            var result = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build())
                .Bind(cfg => Result.Try(() =>
                {
                    var builder = Host.CreateApplicationBuilder();
                    builder.UseConfiguredServiceProviderFactory<HostApplicationBuilder, CustomServiceProviderFactory>(cfg);
                    return builder.Build();
                }));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(CustomServiceProviderFactory.WasCreated);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_WithGenericFactoryType_IgnoresConfiguredFactoryType()
        {
            // Arrange
            CustomServiceProviderFactory.Reset();
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", typeof(TestModule).AssemblyQualifiedName },
                { "serviceProviderFactoryType", "SomeOtherFactory" } // This should be ignored
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            // Act
            var builder = Host.CreateApplicationBuilder();
            var result = builder.UseConfiguredServiceProviderFactory<HostApplicationBuilder, CustomServiceProviderFactory>(configuration);

            // Assert - Should use the generic type parameter and not fail due to invalid config type
            Assert.Same(builder, result);
            Assert.True(CustomServiceProviderFactory.WasCreated);
        }

        [Fact]
        public void UseConfiguredServiceProviderFactory_WithGenericFactoryTypeAndComponents_LoadsModulesFromBoth()
        {
            // Arrange
            CustomServiceProviderFactory.Reset();
            var configDict = new Dictionary<string, string?>
            {
                { "modules:0:type", typeof(TestModule).AssemblyQualifiedName }
            };
            var configuration = new MsConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            IComponent[] ComponentsFactory() => new IComponent[] { new ComponentBuilder.Setup.TestComponent() };

            // Act
            var builder = Host.CreateApplicationBuilder();
            builder.UseConfiguredServiceProviderFactory<HostApplicationBuilder, CustomServiceProviderFactory>(
                configuration, 
                ComponentsFactory);
            var result = builder.Build();

            // Assert
            Assert.NotNull(result);
            Assert.True(CustomServiceProviderFactory.WasCreated);
        }
    }
}
