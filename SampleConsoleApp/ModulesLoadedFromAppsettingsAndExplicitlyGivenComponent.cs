// ============================================================================
// Pattern 2: Hybrid (appsettings.json + IComponent)
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
/// Interface for a logging service - demonstrates code-based module.
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

class LoggerModuleConfiguration : BaseConfiguration
{
    public string Prefix { get; set; } = "LOG";
}

class LoggerModule : BaseModule<LoggerModuleConfiguration>
{
    public LoggerModule(LoggerModuleConfiguration config, List<IModule>? nestedModules = null)
        : base(config, nestedModules) { }

    public override void Load(IServiceCollection services)
    {
        services.AddSingleton<ILoggerService>(new LoggerService(Configuration.Prefix));
        base.Load(services);
    }
}

class LoggerComponent : BaseComponent
{
    protected override FluentResults.Result<ComponentBuilder> Build(ComponentBuilder builder)
    {
        return builder.WithModule<LoggerModule, LoggerModuleConfiguration>(config =>
        {
            config.Prefix = "HYBRID";
        }, cfg => new LoggerModule(cfg));
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
        
        // Module from appsettings.json (ExampleModule with key "example") was loaded
        Console.WriteLine($"  Module from appsettings.json loaded successfully");
        
        // ILoggerService comes from code (LoggerComponent)
        var loggerService = host.Services.GetRequiredService<ILoggerService>();
        loggerService.Log("Module from code component loaded successfully");
        
        await Task.CompletedTask;
    }
}

