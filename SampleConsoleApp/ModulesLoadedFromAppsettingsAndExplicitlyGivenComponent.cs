// ============================================================================
// Pattern 3: Hybrid (appsettings.json + IComponent)
// ============================================================================
// Combines BOTH configuration-based and code-based module loading.
// Components from code are loaded first, then modules from appsettings.json.
// This is the most flexible pattern.
// ============================================================================

using Baubit.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SampleConsoleApp;

/// <summary>
/// Interface for a logging service - different from IGreetingService
/// so we can demonstrate both modules being loaded.
/// </summary>
interface ILoggerService
{
    void Log(string message);
}

class LoggerService : ILoggerService
{
    private readonly string _prefix;

    public LoggerService(string prefix) => _prefix = prefix;

    public void Log(string message) => Console.WriteLine($"  [{_prefix}] {message}");
}

class LoggerModuleConfiguration : AConfiguration
{
    public string Prefix { get; set; } = "LOG";
}

class LoggerModule : AModule<LoggerModuleConfiguration>
{
    public LoggerModule(LoggerModuleConfiguration config, List<IModule>? nestedModules = null)
        : base(config, nestedModules) { }

    public override void Load(IServiceCollection services)
    {
        services.AddSingleton<ILoggerService>(new LoggerService(Configuration.Prefix));
        base.Load(services);
    }
}

class LoggerComponent : AComponent
{
    protected override FluentResults.Result<ComponentBuilder> Build(ComponentBuilder builder)
    {
        return builder.WithModule<LoggerModule, LoggerModuleConfiguration>(config =>
        {
            config.Prefix = "HYBRID";
        });
    }
}

public static class ModulesLoadedFromAppsettingsAndExplicitlyGivenComponent
{
    public static async Task RunAsync()
    {
        // Build host with modules from BOTH appsettings.json AND code
        var builder = Host.CreateApplicationBuilder();
        builder.UseConfiguredServiceProviderFactory(
            componentsFactory: () => [new LoggerComponent()]
        );
        
        using var host = builder.Build();
        
        // IGreetingService comes from appsettings.json (GreetingModule)
        var greetingService = host.Services.GetRequiredService<IGreetingService>();
        Console.WriteLine($"  From appsettings.json: {greetingService.GetGreeting()}");
        
        // ILoggerService comes from code (LoggerComponent)
        var loggerService = host.Services.GetRequiredService<ILoggerService>();
        loggerService.Log("From code component");
        
        await Task.CompletedTask;
    }
}

