using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    /// <summary>
    /// Default service provider factory that uses <see cref="IServiceCollection"/> for dependency injection.
    /// </summary>
    /// <remarks>
    /// This factory loads modules from configuration and components, then registers all services
    /// into the standard .NET dependency injection container. This is the default factory used when
    /// no custom factory type is specified in configuration.
    /// Thread safety: All public members are thread-safe.
    /// </remarks>
    public class ServiceProviderFactory : AServiceProviderFactory<IServiceCollection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class.
        /// </summary>
        /// <param name="defaultServiceProviderFactory">The default .NET service provider factory.</param>
        /// <param name="configuration">The configuration to load modules from.</param>
        /// <param name="components">Optional array of components containing modules to load programmatically.</param>
        public ServiceProviderFactory(DefaultServiceProviderFactory defaultServiceProviderFactory,
                                      IConfiguration configuration,
                                      IComponent[] components) : base(new DefaultServiceProviderFactory(), configuration, components)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class using the default factory.
        /// </summary>
        /// <param name="configuration">The configuration to load modules from.</param>
        /// <param name="components">Optional array of components containing modules to load programmatically.</param>
        public ServiceProviderFactory(IConfiguration configuration, IComponent[] components) : this(new DefaultServiceProviderFactory(), configuration, components)
        {
        }

        /// <summary>
        /// Loads all modules into the service collection.
        /// </summary>
        /// <param name="containerBuilder">The service collection to register services into.</param>
        /// <remarks>
        /// This method iterates through all flattened modules and calls their <see cref="IModule.Load"/> method
        /// to register services into the container.
        /// </remarks>
        public override void Load(IServiceCollection containerBuilder)
        {
            foreach (var module in Modules)
            {
                module.Load(containerBuilder);
            }
        }
    }
}
