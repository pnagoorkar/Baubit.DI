// =============================================================================
// Pattern 3: Hybrid Loading (appsettings.json + IComponent)
// =============================================================================
// This pattern combines BOTH configuration-based and code-based module loading.
// Modules from appsettings.json are loaded alongside modules from IComponent.
//
// Use this pattern when:
// - Some modules need external configuration (database connections, API keys)
// - Some modules need compile-time configuration or code-based logic
// - You want to extend configuration-based modules with additional code-based ones
// 
// Loading order:
// 1. Components from componentsFactory are loaded first
// 2. Modules from appsettings.json "modules" section are loaded second
// =============================================================================

using Microsoft.Extensions.Hosting;
using Baubit.DI;

namespace SampleConsoleApp
{
    public class ModulesLoadedFromAppsettingsAndExplicitlyGivenComponent
    {
        /// <summary>
        /// Demonstrates hybrid loading - combining appsettings.json with IComponent.
        /// 
        /// This approach allows you to:
        /// - Load modules defined in appsettings.json
        /// - Add additional modules defined in code via componentsFactory
        /// - Override or extend configuration-based behavior
        /// </summary>
        public static async Task RunAsync()
        {
            // CreateApplicationBuilder loads appsettings.json
            // componentsFactory adds additional modules from code
            // Both sources of modules are combined
            await Host.CreateApplicationBuilder()
                      .UseConfiguredServiceProviderFactory(componentsFactory: BuildComponents)
                      .Build()
                      .RunAsync();
        }

        /// <summary>
        /// Factory method that creates additional IComponent instances.
        /// These components are loaded alongside modules from appsettings.json.
        /// </summary>
        private static IComponent[] BuildComponents()
        {
            // This component adds modules programmatically
            // They will be loaded IN ADDITION TO modules from appsettings.json
            return [new MyComponent()];
        }
    }
}

