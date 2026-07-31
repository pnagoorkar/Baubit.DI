using Baubit.DI.Test.ServiceProviderFactory.Setup;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI.Test.HostBuilderExtensions
{
    /// <summary>
    /// Unit tests for <see cref="DI.HostBuilderExtensions"/>
    /// </summary>
    public class Test
    {
        // ---------------------------------------------------------------
        // WithDefaultServiceProviderFactory — no arguments
        // ---------------------------------------------------------------

        [Fact]
        public void WithDefaultServiceProviderFactory_WithNoArguments_ReturnsSameBuilderInstance()
        {
            var builder = Host.CreateApplicationBuilder();

            var returned = builder.WithDefaultServiceProviderFactory();

            Assert.Same(builder, returned);
        }

        [Fact]
        public void WithDefaultServiceProviderFactory_WithNoArguments_BuildsSuccessfully()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory()
                           .Build();

            Assert.NotNull(host);
        }

        [Fact]
        public void WithDefaultServiceProviderFactory_WithNoArguments_ServiceProviderIsUsable()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory()
                           .Build();

            Assert.NotNull(host.Services.GetService<IHostApplicationLifetime>());
        }

        // ---------------------------------------------------------------
        // WithDefaultServiceProviderFactory — additionalConfigurations
        // ---------------------------------------------------------------

        [Theory]
        [InlineData("Baubit.DI.Test;ServiceProviderFactory.Setup.config.json")]
        public void WithDefaultServiceProviderFactory_WithAdditionalConfig_ReturnsSameBuilderInstance(string configFile)
        {
            var cfg = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build()).Value;

            var builder = Host.CreateApplicationBuilder();

            var returned = builder.WithDefaultServiceProviderFactory(additionalConfigurations: [cfg]);

            Assert.Same(builder, returned);
        }

        [Theory]
        [InlineData("Baubit.DI.Test;ServiceProviderFactory.Setup.config.json")]
        public void WithDefaultServiceProviderFactory_WithAdditionalConfig_LoadsModulesFromConfig(string configFile)
        {
            var result = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build())
                .Bind(cfg => Result.Try(() =>
                    Host.CreateApplicationBuilder()
                        .WithDefaultServiceProviderFactory(additionalConfigurations: [cfg])
                        .Build()));

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Services.GetService<MyComponent>());
        }

        [Fact]
        public void WithDefaultServiceProviderFactory_WithNullAdditionalConfigurations_FallsBackToHostConfig()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory(additionalConfigurations: null)
                           .Build();

            Assert.NotNull(host);
        }

        // ---------------------------------------------------------------
        // WithDefaultServiceProviderFactory — components
        // ---------------------------------------------------------------

        [Fact]
        public void WithDefaultServiceProviderFactory_WithComponent_LoadsModulesFromComponent()
        {
            var componentResult = Baubit.DI.ComponentBuilder.CreateNew()
                .Bind(b => b.WithModule<TestModule, TestConfiguration>(
                    (Action<TestConfiguration>)(c => { }),
                    c => new TestModule(c, null)))
                .Bind(b => b.Build());

            Assert.True(componentResult.IsSuccess);

            using var component = componentResult.Value;

            var host = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings())
                           .WithDefaultServiceProviderFactory(components: [component])
                           .Build();

            Assert.NotNull(host.Services.GetService<MyComponent>());
        }

        [Fact]
        public void WithDefaultServiceProviderFactory_WithEmptyComponentsArray_BuildsSuccessfully()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory(components: [])
                           .Build();

            Assert.NotNull(host);
        }

        [Fact]
        public void WithDefaultServiceProviderFactory_WithNullComponents_BuildsSuccessfully()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory(components: null)
                           .Build();

            Assert.NotNull(host);
        }

        // ---------------------------------------------------------------
        // WithDefaultServiceProviderFactory — configure action
        // ---------------------------------------------------------------

        [Fact]
        public void WithDefaultServiceProviderFactory_WithConfigureAction_RegistrationIsApplied()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory(configure: services =>
                               services.AddSingleton<MyComponent>())
                           .Build();

            Assert.NotNull(host.Services.GetService<MyComponent>());
        }

        [Fact]
        public void WithDefaultServiceProviderFactory_WithNullConfigureAction_BuildsSuccessfully()
        {
            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory(configure: null)
                           .Build();

            Assert.NotNull(host);
        }

        // ---------------------------------------------------------------
        // WithDefaultServiceProviderFactory — hybrid (config + component)
        // ---------------------------------------------------------------

        [Theory]
        [InlineData("Baubit.DI.Test;ServiceProviderFactory.Setup.config.json")]
        public void WithDefaultServiceProviderFactory_HybridConfigAndComponent_LoadsBothSources(string configFile)
        {
            var cfg = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                .Bind(cb => cb.Build()).Value;

            var componentResult = Baubit.DI.ComponentBuilder.CreateNew()
                .Bind(b => b.WithModule<TestModule, TestConfiguration>(
                    (Action<TestConfiguration>)(c => { }),
                    c => new TestModule(c, null)))
                .Bind(b => b.Build());

            Assert.True(componentResult.IsSuccess);

            using var component = componentResult.Value;

            var host = Host.CreateApplicationBuilder()
                           .WithDefaultServiceProviderFactory(
                               additionalConfigurations: [cfg],
                               components: [component])
                           .Build();

            Assert.NotNull(host.Services.GetService<MyComponent>());
        }

        // ---------------------------------------------------------------
        // WithServiceProviderFactory — existing behaviour preserved
        // ---------------------------------------------------------------

        [Fact]
        public void WithServiceProviderFactory_WithDefaultFactory_ReturnsSameBuilderInstance()
        {
            var builder = Host.CreateApplicationBuilder();
            var factory = new DI.ServiceProviderFactory(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

            var returned = builder.WithServiceProviderFactory(factory);

            Assert.Same(builder, returned);
        }

        [Fact]
        public void WithServiceProviderFactory_WithConfigureAction_RegistrationIsApplied()
        {
            var factory = new DI.ServiceProviderFactory(
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

            var host = Host.CreateApplicationBuilder()
                           .WithServiceProviderFactory(factory, services =>
                               services.AddSingleton<MyComponent>())
                           .Build();

            Assert.NotNull(host.Services.GetService<MyComponent>());
        }
    }
}
