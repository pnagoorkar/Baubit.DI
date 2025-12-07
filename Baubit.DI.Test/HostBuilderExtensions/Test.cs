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
        public void UseConfiguredServiceProviderFactory_WithValidConfig_BuildsHost(string configFile)
        {
            // Arrange & Act
            var result = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build())
                .Bind(cfg => Result.Try(() => Host.CreateApplicationBuilder().UseConfiguredServiceProviderFactory(cfg).Build()));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value.Services);
            Assert.IsType<TestComponent>(result.Value.Services.GetRequiredService<TestComponent>());
        }

        [Theory]
        [InlineData("Baubit.DI.Test;HostBuilderExtensions.Setup.customFactoryType.json")]
        public void UseConfiguredServiceProviderFactory_WithCustomFactoryType_UsesCustomFactory(string configFile)
        {
            // Arrange
            CustomServiceProviderFactory.Reset();

            // Act
            var result = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build())
                .Bind(cfg => Result.Try(() => Host.CreateApplicationBuilder().UseConfiguredServiceProviderFactory(cfg).Build()));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(CustomServiceProviderFactory.WasCreated);
        }

        [Theory]
        [InlineData("Baubit.DI.Test;HostBuilderExtensions.Setup.invalidFactoryType.json")]
        public void UseConfiguredServiceProviderFactory_WithInvalidFactoryType_CallsOnFailure(string configFile)
        {
            // Arrange
            bool onFailureCalled = false;
            IResultBase? capturedResult = null;

            void OnFailure<T>(T builder, IResultBase result) where T : IHostApplicationBuilder
            {
                onFailureCalled = true;
                capturedResult = result;
            }

            // Act
            var configResult = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build());

            Assert.True(configResult.IsSuccess);
            
            var builder = Host.CreateApplicationBuilder();
            builder.UseConfiguredServiceProviderFactory(configResult.Value, null, OnFailure);

            // Assert
            Assert.True(onFailureCalled);
            Assert.NotNull(capturedResult);
            Assert.True(capturedResult.IsFailed);
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
