using System.Collections.Generic;

namespace Baubit.DI
{
    /// <summary>
    /// Extends <see cref="Microsoft.Extensions.DependencyInjection.IServiceProviderFactory{TContainerBuilder}"/> with
    /// module-based dependency injection capabilities.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of container builder used by the factory.</typeparam>
    public interface IServiceProviderFactory<TContainerBuilder> : Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder>
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
