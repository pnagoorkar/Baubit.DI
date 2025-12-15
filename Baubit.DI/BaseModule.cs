using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Baubit.DI
{
    /// <summary>
    /// Abstract base class for dependency injection modules.
    /// </summary>
    /// <remarks>
    /// Modules encapsulate service registrations and can be composed hierarchically.
    /// Derive from this class or <see cref="BaseModule{TConfiguration}"/> to create custom modules.
    /// </remarks>
    public abstract class BaseModule : IModule
    {
        /// <summary>
        /// Gets or sets the configuration associated with this module.
        /// </summary>
        [JsonIgnore]
        public BaseConfiguration Configuration { get; protected set; }

        /// <summary>
        /// Gets the collection of nested modules that this module depends on.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<IModule> NestedModules { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModule"/> class.
        /// </summary>
        /// <param name="configuration">The configuration for this module.</param>
        /// <param name="nestedModules">The list of nested modules this module depends on.</param>
        public BaseModule(BaseConfiguration configuration, List<IModule> nestedModules)
        {
            Configuration = configuration;
            NestedModules = nestedModules.Concat(GetKnownDependencies()).ToList().AsReadOnly();
            OnInitialized();
        }

        /// <summary>
        /// Called by the constructor in <see cref="BaseModule"/> after all construction activities.
        /// Override this method to perform construction in child types.
        /// </summary>
        protected virtual void OnInitialized()
        {

        }

        /// <summary>
        /// Override to provide known module dependencies that should be added to <see cref="NestedModules"/>.
        /// </summary>
        /// <returns>An enumerable of modules that this module depends on.</returns>
        protected virtual IEnumerable<BaseModule> GetKnownDependencies() => Enumerable.Empty<BaseModule>();



        /// <summary>
        /// Registers services with the specified service collection.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <remarks>
        /// Override this method in derived classes to register services.
        /// The base implementation does nothing because modules are flattened by the service provider factory,
        /// and each flattened module's Load method is called individually to register its services.
        /// </remarks>
        public virtual void Load(IServiceCollection services)
        {
            // NO ACTION NEEDED. 
            // Modules are flattened by the service provider factory,
            // and each module's Load method is called individually.
        }
    }

    /// <summary>
    /// Abstract base class for dependency injection modules with strongly-typed configuration.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of configuration for this module.</typeparam>
    public abstract class BaseModule<TConfiguration> : BaseModule where TConfiguration : BaseConfiguration
    {
        /// <summary>
        /// Gets the strongly-typed configuration associated with this module.
        /// </summary>
        public new TConfiguration Configuration
        {
            get => (TConfiguration)base.Configuration;
            private set => base.Configuration = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModule{TConfiguration}"/> class.
        /// </summary>
        /// <param name="configuration">The strongly-typed configuration for this module.</param>
        /// <param name="nestedModules">Optional list of nested modules this module depends on.</param>
        protected BaseModule(TConfiguration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules ?? new List<IModule>())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModule{TConfiguration}"/> class from an <see cref="IConfiguration"/> section.
        /// </summary>
        /// <param name="configuration">The configuration section to bind settings from.</param>
        protected BaseModule(IConfiguration configuration) : this(configuration.Get<TConfiguration>(), LoadNestedModules(configuration))
        {

        }

        private static List<IModule> LoadNestedModules(IConfiguration configuration)
        {
            return ModuleBuilder.CreateMany(configuration)
                                .Bind(moduleBuilders => Result.Try(() => moduleBuilders.Select(module => module.Build().ThrowIfFailed().Value).ToList()))
                                .ThrowIfFailed()
                                .Value;
        }
    }
}
