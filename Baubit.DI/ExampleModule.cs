using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Baubit.DI
{
    /// <summary>
    /// Example module configuration for testing.
    /// </summary>
    public class ExampleModuleConfiguration : BaseConfiguration
    {
        public string Message { get; set; } = "Default message";
    }

    /// <summary>
    /// Example module demonstrating secure module loading with BaubitModuleAttribute.
    /// This module can be loaded from configuration using the key "example".
    /// </summary>
    /// <example>
    /// Configuration example:
    /// {
    ///   "modules": [
    ///     {
    ///       "type": "example",
    ///       "configuration": {
    ///         "Message": "Hello World!"
    ///       }
    ///     }
    ///   ]
    /// }
    /// </example>
    [BaubitModule("example")]
    public class ExampleModule : BaseModule<ExampleModuleConfiguration>
    {
        /// <summary>
        /// Initializes a new instance from configuration.
        /// </summary>
        /// <param name="configuration">The configuration section.</param>
        public ExampleModule(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance programmatically.
        /// </summary>
        /// <param name="configuration">The module configuration.</param>
        /// <param name="nestedModules">Optional nested modules.</param>
        public ExampleModule(ExampleModuleConfiguration configuration, List<IModule> nestedModules = null)
            : base(configuration, nestedModules)
        {
        }

        /// <summary>
        /// Registers services for this module.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public override void Load(IServiceCollection services)
        {
            // Example: Register a singleton with the configured message
            // services.AddSingleton(new ExampleService(Configuration.Message));
            base.Load(services);
        }
    }
}
