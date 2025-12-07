using FluentResults;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace Baubit.DI
{
    /// <summary>
    /// Interface for service provider factories that can be configured via host application builders.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface provide a way to configure the service provider
    /// using modules loaded from configuration.
    /// </remarks>
    public interface IServiceProviderFactory
    {
        /// <summary>
        /// Configures the host application builder to use this service provider factory.
        /// </summary>
        /// <typeparam name="THostApplicationBuilder">The type of host application builder.</typeparam>
        /// <param name="hostApplicationBuilder">The host application builder to configure.</param>
        /// <returns>A result containing the configured host application builder, or failure information.</returns>
        Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
    }

    /// <summary>
    /// Generic interface for service provider factories that integrate module-based dependency injection with custom container builders.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of container builder used by the factory.</typeparam>
    /// <remarks>
    /// This interface extends <see cref="IServiceProviderFactory"/> to provide access to the internal factory,
    /// loaded modules, and a method to load modules into the container builder.
    /// </remarks>
    public interface IServiceProviderFactory<TContainerBuilder> : IServiceProviderFactory
    {
        /// <summary>
        /// Gets the internal service provider factory that is wrapped by this instance.
        /// </summary>
        Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> InternalFactory { get; }

        /// <summary>
        /// Gets the flattened collection of all modules loaded from configuration and components.
        /// </summary>
        List<IModule> Modules { get; }

        /// <summary>
        /// Loads all modules into the specified container builder.
        /// </summary>
        /// <param name="containerBuilder">The container builder to load modules into.</param>
        void Load(TContainerBuilder containerBuilder);
    }
}
