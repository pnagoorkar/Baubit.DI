// ============================================================================
// Baubit.DI Sample Console Application
// ============================================================================
// This application demonstrates three patterns for loading DI modules:
//   Pattern 1: From appsettings.json only (secure module registry)
//   Pattern 2: From code only (IComponent)
//   Pattern 3: Hybrid - both appsettings.json AND code
//
// The GreetingModule uses [BaubitModule("greeting")] to enable secure loading
// from configuration files.
// ============================================================================

using Baubit.DI;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleConsoleApp;

// Register modules from this assembly with the secure module registry
// Note: Ideally SampleModuleRegistry.Register() would be called here once the generator works
// For now, we manually register the GreetingModule
ModuleRegistry.RegisterExternal(dict =>
{
    dict["greeting"] = cfg => new GreetingModule(cfg);
});

Console.WriteLine("=== Baubit.DI Sample Application ===\n");

// Run each pattern sequentially so output is clear
Console.WriteLine("--- Pattern 1: Modules from appsettings.json ---");
await ModulesLoadedFromAppsettings.RunAsync();

Console.WriteLine("\n--- Pattern 2: Modules from Code (IComponent) ---");
await ModulesLoadedFromExplicitlyGivenComponent.RunAsync();

Console.WriteLine("\n--- Pattern 3: Hybrid (appsettings.json + IComponent) ---");
await ModulesLoadedFromAppsettingsAndExplicitlyGivenComponent.RunAsync();

Console.WriteLine("\n=== All patterns completed ===");

// ============================================================================
// SERVICES
// ============================================================================

/// <summary>
/// Interface for the greeting service - allows verification of which module registered it.
/// </summary>
public interface IGreetingService
{
    string GetGreeting();
}

/// <summary>
/// A simple greeting service that returns a configured message.
/// </summary>
public class GreetingService : IGreetingService
{
    private readonly string _message;

    public GreetingService(string message)
    {
        _message = message;
    }

    public string GetGreeting() => _message;
}

// ============================================================================
// MODULE CONFIGURATION
// ============================================================================

/// <summary>
/// Configuration for GreetingModule.
/// - When loaded from appsettings.json, properties are bound automatically
/// - When created in code, properties are set directly
/// </summary>
public class GreetingModuleConfiguration : BaseConfiguration
{
    public string Message { get; set; } = "Default greeting";
}

// ============================================================================
// MODULE DEFINITION
// ============================================================================

/// <summary>
/// A module that registers IGreetingService.
/// Demonstrates:
/// - Module attribute for secure loading: [BaubitModule("greeting")]
/// - Two constructors (IConfiguration vs typed configuration)
/// - Service registration in Load()
/// - Calling base.Load() for nested modules
/// Configuration example: { "type": "greeting", "configuration": { "Message": "Hello!" } }
/// </summary>
[BaubitModule("greeting")]
public class GreetingModule : BaseModule<GreetingModuleConfiguration>
{
    // Constructor for loading from appsettings.json
    public GreetingModule(IConfiguration configuration) : base(configuration) { }

    // Constructor for programmatic creation
    public GreetingModule(GreetingModuleConfiguration configuration, List<IModule>? nestedModules = null)
        : base(configuration, nestedModules) { }

    public override void Load(IServiceCollection services)
    {
        services.AddSingleton<IGreetingService>(new GreetingService(Configuration.Message));
        base.Load(services); // Always call to load nested modules
    }
}

// ============================================================================
// COMPONENT DEFINITION
// ============================================================================

/// <summary>
/// A component that creates GreetingModule in code with a custom message.
/// </summary>
public class CodeGreetingComponent : BaseComponent
{
    private readonly string _message;

    public CodeGreetingComponent(string message)
    {
        _message = message;
    }

    protected override Result<ComponentBuilder> Build(ComponentBuilder componentBuilder)
    {
        return componentBuilder.WithModule<GreetingModule, GreetingModuleConfiguration>(config =>
        {
            config.Message = _message;
        }, cfg => new GreetingModule(cfg));
    }
}

