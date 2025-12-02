// =============================================================================
// Pattern 2: Loading Modules Purely from Code (IComponent)
// =============================================================================
// This pattern loads ALL modules programmatically using IComponent.
// No configuration file is needed - all modules are defined in code.
//
// Use this pattern when:
// - Module configuration is determined at compile time
// - You need full control over module instantiation
// - Configuration values come from code, not files
// =============================================================================

using Microsoft.Extensions.Hosting;
using Baubit.DI;

namespace SampleConsoleApp
{
    public class ModulesLoadedFromExplicitlyGivenComponent
    {
        /// <summary>
        /// Demonstrates loading modules purely from code using IComponent.
        /// 
        /// Key points:
        /// - CreateEmptyApplicationBuilder() creates a builder without appsettings.json
        /// - componentsFactory parameter provides the modules via IComponent instances
        /// - Each IComponent's Build method adds modules to the ComponentBuilder
        /// </summary>
        public static async Task RunAsync()
        {
            // CreateEmptyApplicationBuilder doesn't load appsettings.json
            // The componentsFactory parameter supplies all modules from code
            await Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings())
                      .UseConfiguredServiceProviderFactory(componentsFactory: BuildComponents)
                      .Build()
                      .RunAsync();
        }

        /// <summary>
        /// Factory method that creates IComponent instances.
        /// Each component can define one or more modules programmatically.
        /// </summary>
        private static IComponent[] BuildComponents()
        {
            // MyComponent.Build() will be called to add modules
            return [new MyComponent()];
        }
    }
}

