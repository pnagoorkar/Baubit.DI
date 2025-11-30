using Baubit.Configuration;
using Baubit.DI.Test.ServiceProviderFactory.Setup;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI.Test.ServiceProviderFactory
{
    public class Test
    {
        [Theory]
        [InlineData("Baubit.DI.Test;ServiceProviderFactory.Setup.config.json")]
        public void CanRun(string configFile)
        {
            var result = ConfigurationBuilder.CreateNew()
                                                    .Bind(cb => cb.WithEmbeddedJsonResources(configFile))
                                                    .Bind(cb => cb.Build())
                                                    .Bind(cfg => Result.Try(() => Host.CreateApplicationBuilder().UseConfiguredServiceProviderFactory(cfg).Build()));

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value.Services);
            Assert.IsType<MyComponent>(result.Value.Services.GetRequiredService<MyComponent>());
        }
    }
}
