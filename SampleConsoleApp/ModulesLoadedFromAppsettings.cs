// =============================================================================
// Pattern 1: Loading Modules Purely from appsettings.json
// =============================================================================
// This pattern loads ALL modules from configuration (appsettings.json).
// Module types, their configurations, and nested modules are defined in JSON.
//
// Use this pattern when:
// - Module configuration should be externally configurable
// - You want to change module behavior without recompiling
// - All module settings can be expressed in configuration
// =============================================================================

using Microsoft.Extensions.Hosting;
using Baubit.DI;

namespace SampleConsoleApp
{
    public class ModulesLoadedFromAppsettings
    {
        /// <summary>
        /// Demonstrates loading modules purely from appsettings.json.
        /// 
        /// The modules array in appsettings.json defines:
        /// - Module type (assembly-qualified name)
        /// - Module configuration (in "configuration" section)
        /// - Nested modules (if any)
        /// </summary>
        public static async Task RunAsync()
        {
            // CreateApplicationBuilder loads appsettings.json automatically
            // UseConfiguredServiceProviderFactory reads the "modules" section
            // and creates module instances from the configuration
            await Host.CreateApplicationBuilder()
                      .UseConfiguredServiceProviderFactory()
                      .Build()
                      .RunAsync();
        }
    }
}

