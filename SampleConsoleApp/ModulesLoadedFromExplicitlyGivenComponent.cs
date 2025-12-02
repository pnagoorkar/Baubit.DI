// ============================================================================
// Pattern 2: Modules from Code (IComponent)
// ============================================================================
// All modules are defined in code - no appsettings.json modules.
// Uses IComponent to define modules programmatically.
// ============================================================================

using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleConsoleApp;

public static class ModulesLoadedFromExplicitlyGivenComponent
{
    public static async Task RunAsync()
    {
        // Build host with modules from code only (no appsettings.json)
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.UseConfiguredServiceProviderFactory(
            componentsFactory: () => [new CodeGreetingComponent("Hello from code component!")]
        );
        
        using var host = builder.Build();
        
        // Verify the module was loaded from code
        var greetingService = host.Services.GetRequiredService<IGreetingService>();
        Console.WriteLine($"  {greetingService.GetGreeting()}");
        
        await Task.CompletedTask;
    }
}

