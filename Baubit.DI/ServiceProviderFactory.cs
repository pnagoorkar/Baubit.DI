using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    /// <summary>
    /// Default implementation of <see cref="IServiceProviderFactory"/> that loads modules from configuration.
    /// </summary>
    /// <remarks>
    /// This factory extends <see cref="DefaultServiceProviderFactory"/> and loads modules
    /// defined in the configuration's "modules" and "moduleSources" sections.
    /// </remarks>
    public class ServiceProviderFactory : DefaultServiceProviderFactory, IServiceProviderFactory
    {
        private readonly List<IModule> modules = new List<IModule>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class
        /// with default options.
        /// </summary>
        /// <param name="configuration">Host builder configuration containing module definitions.</param>
        /// <param name="components">Optional array of pre-built components to include.</param>
        public ServiceProviderFactory(IConfiguration configuration, IComponent[] components) : base()
        {
            Initialize(configuration, components);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class
        /// with the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The service provider options to use for this instance.</param>
        /// <param name="configuration">Host builder configuration containing module definitions.</param>
        /// <param name="components">Optional array of pre-built components to include.</param>
        public ServiceProviderFactory(ServiceProviderOptions options, IConfiguration configuration, IComponent[] components) : base(options)
        {
            Initialize(configuration, components);
        }

        private void Initialize(IConfiguration configuration, IComponent[] components)
        {
            if (components != null)
            {
                modules.AddRange(components.SelectMany(component => component));
            }
            LoadModules(configuration);
        }

        private void LoadModules(IConfiguration configuration)
        {
            ModuleBuilder.CreateMany(configuration)
                         .Bind(moduleBuilders => Result.Try(() => modules.AddRange(moduleBuilders.Select(moduleBuilder => moduleBuilder.Build().ThrowIfFailed().Value))));
        }

        /// <summary>
        /// Loads all configured modules into the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to load modules into.</param>
        public void Load(IServiceCollection services)
        {
            foreach (var module in modules)
            {
                module.Load(services);
            }
        }

        /// <summary>
        /// Configures the host application builder to use this service provider factory.
        /// </summary>
        /// <typeparam name="THostApplicationBuilder">The type of host application builder.</typeparam>
        /// <param name="hostApplicationBuilder">The host application builder to configure.</param>
        /// <returns>A result containing the configured host application builder.</returns>
        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(this, Load);
            return hostApplicationBuilder;
        }
    }
}
