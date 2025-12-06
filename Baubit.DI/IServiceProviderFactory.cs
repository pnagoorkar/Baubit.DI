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

    public interface IServiceProviderFactory<TContainerBuilder> : IServiceProviderFactory
    {
        Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> InternalFactory { get; }
        List<IModule> Modules { get; }
        void Load(TContainerBuilder containerBuilder);
    }
}
