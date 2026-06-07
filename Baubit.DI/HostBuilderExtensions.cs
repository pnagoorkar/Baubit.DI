using Baubit.Configuration;
using Baubit.Traceability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// Configures the host builder to use the default <see cref="ServiceProviderFactory"/> with module-based dependency injection.
        /// </summary>
        /// <typeparam name="THostBuilder">The type of host application builder.</typeparam>
        /// <param name="hostBuilder">The host application builder to configure.</param>
        /// <param name="additionalConfigurations">
        /// Optional additional <see cref="IConfiguration"/> sources to overlay on top of
        /// <see cref="IHostApplicationBuilder.Configuration"/>.
        /// When provided, the host builder's own configuration is merged with these sources so that
        /// all existing host settings are preserved and the additional sources can supply or override module definitions.
        /// When <see langword="null"/>, <see cref="IHostApplicationBuilder.Configuration"/> is used directly.
        /// </param>
        /// <param name="components">
        /// Optional array of <see cref="IComponent"/> instances whose modules are loaded programmatically.
        /// Component modules are registered before any configuration-based modules.
        /// </param>
        /// <param name="configure">
        /// An optional action to perform additional configuration on the <see cref="IServiceCollection"/> after all
        /// module services have been registered.
        /// </param>
        /// <returns>The same <paramref name="hostBuilder"/> instance for fluent chaining.</returns>
        /// <remarks>
        /// This is the recommended entry point for standard .NET dependency injection scenarios.
        /// Use <see cref="WithServiceProviderFactory{THostBuilder, TContainerBuilder}"/> only when integrating
        /// a custom third-party container (e.g., Autofac).
        /// <para>
        /// Module loading order:
        /// <list type="number">
        ///   <item><description>Modules from <paramref name="components"/> (if provided)</description></item>
        ///   <item><description>Modules from the resolved configuration (merged host config + <paramref name="additionalConfigurations"/>, or host config alone)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public static THostBuilder WithDefaultServiceProviderFactory<THostBuilder>(this THostBuilder hostBuilder,
                                                                                   IConfiguration[] additionalConfigurations = null,
                                                                                   IComponent[] components = null,
                                                                                   Action<IServiceCollection> configure = null) where THostBuilder : IHostApplicationBuilder
        {
            var cfg = default(IConfiguration);
            if (additionalConfigurations != null)
            {
                cfg = Baubit.Configuration.ConfigurationBuilder.CreateNew()
                                                               .WithAdditionalConfigurations(hostBuilder.Configuration)
                                                               .WithAdditionalConfigurations(additionalConfigurations)
                                                               .Build()
                                                               .ThrowIfFailed()
                                                               .Value;
            }
            else
            {
                cfg = hostBuilder.Configuration;
            }
            var serviceProviderFactory = new ServiceProviderFactory(cfg, components);
            return hostBuilder.WithServiceProviderFactory(serviceProviderFactory, configure);
        }
    }
}
