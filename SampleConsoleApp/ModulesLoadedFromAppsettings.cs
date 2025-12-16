// ============================================================================
// Pattern 1: Modules from appsettings.json
// ============================================================================
// All modules are defined in configuration - no code-based modules.
// Module types, configurations, and nested modules come from JSON.
// ============================================================================

using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleConsoleApp;

public static class ModulesLoadedFromAppsettings
{
    public static async Task RunAsync()
    {
        // Build host with modules from appsettings.json only
        var builder = Host.CreateApplicationBuilder();
        builder.UseConfiguredServiceProviderFactory();
        
        using var host = builder.Build();
        
        // Module was loaded successfully from appsettings.json using secure module registry
        Console.WriteLine($"  Module loaded successfully from appsettings.json using [BaubitModule] attribute");
        
        await Task.CompletedTask;
    }
}

