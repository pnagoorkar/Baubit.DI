using Microsoft.Extensions.Hosting;
using System;

namespace Baubit.DI
{
    /// <summary>
    /// Extension methods for <see cref="IHostApplicationBuilder"/> to configure module-based dependency injection.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures the host builder to use the specified service provider factory with module-based dependency injection.
        /// </summary>
        /// <typeparam name="THostBuilder">The type of host application builder.</typeparam>
        /// <typeparam name="TContainerBuilder">The type of container builder used by the factory.</typeparam>
        /// <param name="hostBuilder">The host application builder to configure.</param>
        /// <param name="serviceProviderFactory">The service provider factory to use for dependency injection.</param>
        /// <param name="configure">An optional action to perform additional configuration on the container builder.</param>
        /// <returns>The same <paramref name="hostBuilder"/> instance for fluent chaining.</returns>
        public static THostBuilder WithServiceProviderFactory<THostBuilder, TContainerBuilder>(this THostBuilder hostBuilder,
                                                                                                IServiceProviderFactory<TContainerBuilder> serviceProviderFactory,
                                                                                                Action<TContainerBuilder> configure = null) where THostBuilder : IHostApplicationBuilder
        {
            hostBuilder.ConfigureContainer(serviceProviderFactory, configure);
            return hostBuilder;
        }
    }
}
