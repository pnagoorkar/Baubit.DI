using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Baubit.DI
{
    /// <summary>
    /// Defines the contract for a dependency injection module that can register services.
    /// </summary>
    /// <remarks>
    /// Modules encapsulate service registrations and can be composed hierarchically through nested modules.
    /// </remarks>
    public interface IModule
    {
        /// <summary>
        /// Gets the configuration associated with this module.
        /// </summary>
        BaseConfiguration Configuration { get; }

        /// <summary>
        /// Gets the collection of nested modules that this module depends on.
        /// </summary>
        IReadOnlyList<IModule> NestedModules { get; }

        /// <summary>
        /// Registers services with the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        void Load(IServiceCollection services);
    }
}
