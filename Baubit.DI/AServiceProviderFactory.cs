using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    /// <summary>
    /// Abstract base class for service provider factories that integrate module-based dependency injection with custom container builders.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of container builder to use (e.g., IServiceCollection for default .NET DI).</typeparam>
    /// <remarks>
    /// This class provides a foundation for creating custom service provider factories that load modules from
    /// both configuration files and programmatically defined components. Modules are flattened and loaded into
    /// the container builder via the abstract <see cref="Load"/> method.
    /// Thread safety: All public members are thread-safe.
    /// </remarks>
    public abstract class AServiceProviderFactory<TContainerBuilder> : IServiceProviderFactory<TContainerBuilder>
    {
        /// <summary>
        /// Gets the internal service provider factory that is wrapped by this instance.
        /// </summary>
        /// <remarks>
        /// This factory is used by the host application builder to configure the dependency injection container.
        /// </remarks>
        public Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> InternalFactory { get; }

        /// <summary>
        /// Gets the flattened collection of all modules loaded from configuration and components.
        /// </summary>
        /// <remarks>
        /// This collection includes all modules recursively flattened from nested module hierarchies.
        /// Modules are loaded in the order they are defined: components first, then configuration-based modules.
        /// </remarks>
        public List<IModule> Modules { get; } = new List<IModule>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AServiceProviderFactory{TContainerBuilder}"/> class.
        /// </summary>
        /// <param name="internalFactory">The internal service provider factory to wrap.</param>
        /// <param name="configuration">The configuration to load modules from.</param>
        /// <param name="components">Optional array of components containing modules to load programmatically.</param>
        /// <remarks>
        /// Modules are loaded in the following order:
        /// 1. Modules from components (if provided)
        /// 2. Modules from configuration
        /// All modules are then flattened to resolve nested module hierarchies.
        /// </remarks>
        public AServiceProviderFactory(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> internalFactory,
                                       IConfiguration configuration,
                                       IComponent[] components)
        {
            InternalFactory = internalFactory;
            Initialize(configuration, components);
        }

        /// <summary>
        /// Initializes the factory by loading modules from components and configuration.
        /// </summary>
        /// <param name="configuration">The configuration to load modules from.</param>
        /// <param name="components">The components containing modules to load.</param>
        private void Initialize(IConfiguration configuration, IComponent[] components)
        {
            var modules = new List<IModule>();
            if (components != null)
            {
                modules.AddRange(components.SelectMany(component => component));
            }
            LoadModules(configuration, modules);

            Modules.AddRange(modules.SelectMany(module => module.TryFlatten().ThrowIfFailed().Value));
        }

        /// <summary>
        /// Loads modules from configuration and adds them to the provided list.
        /// </summary>
        /// <param name="configuration">The configuration to load modules from.</param>
        /// <param name="modules">The list to add loaded modules to.</param>
        private void LoadModules(IConfiguration configuration, List<IModule> modules)
        {
            ModuleBuilder.CreateMany(configuration)
                         .Bind(moduleBuilders => Result.Try(() => modules.AddRange(moduleBuilders.Select(moduleBuilder => moduleBuilder.Build().ThrowIfFailed().Value))));
        }

        /// <summary>
        /// Loads all modules into the container builder.
        /// </summary>
        /// <param name="containerBuilder">The container builder to load modules into.</param>
        /// <remarks>
        /// Override this method to implement custom loading logic for the specific container type.
        /// This method is called by the host application builder during container configuration.
        /// </remarks>
        public abstract void Load(TContainerBuilder containerBuilder);

        /// <summary>
        /// Configures the host application builder to use this service provider factory.
        /// </summary>
        /// <typeparam name="THostApplicationBuilder">The type of host application builder.</typeparam>
        /// <param name="hostApplicationBuilder">The host application builder to configure.</param>
        /// <returns>A result containing the configured host application builder, or failure information.</returns>
        /// <remarks>
        /// This method configures the host builder to use the internal factory and load method.
        /// </remarks>
        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(InternalFactory, Load);
            return hostApplicationBuilder;
        }
    }
}
