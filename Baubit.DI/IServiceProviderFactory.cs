using System.Collections.Generic;

namespace Baubit.DI
{
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
