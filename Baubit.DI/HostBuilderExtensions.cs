using Microsoft.Extensions.Hosting;
using System;

namespace Baubit.DI
{
    public static class HostBuilderExtensions
    {
        public static THostBuilder WithServiceProviderFactory<THostBuilder, TContainerBuilder>(this THostBuilder hostBuilder,
                                                                                                IServiceProviderFactory<TContainerBuilder> serviceProviderFactory,
                                                                                                Action<TContainerBuilder> configure = null) where THostBuilder : IHostApplicationBuilder
        {
            hostBuilder.ConfigureContainer(serviceProviderFactory, configure);
            return hostBuilder;
        }
    }
}
