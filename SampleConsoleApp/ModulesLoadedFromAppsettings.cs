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
        var hostAppBuilderSettings = new HostApplicationBuilderSettings
        {
            Args = Array.Empty<string>(),
            ContentRootPath = AppContext.BaseDirectory
        };
        // Build host with modules from appsettings.json only
        var builder = Host.CreateApplicationBuilder(hostAppBuilderSettings);
        using var host = builder.WithServiceProviderFactory(new ServiceProviderFactory(builder.Configuration)).Build();

        // Verify the greeting module was loaded from appsettings.json
        var greetingService = host.Services.GetRequiredService<IGreetingService>();
        Console.WriteLine($"  {greetingService.GetGreeting()}");

        await Task.CompletedTask;
    }
}
