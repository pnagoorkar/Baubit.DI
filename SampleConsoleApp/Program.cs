// =============================================================================
// SampleConsoleApp - Baubit.DI Usage Examples
// =============================================================================
// This file demonstrates three patterns for using Baubit.DI:
// 1. Loading modules purely from appsettings.json
// 2. Loading modules purely from code (using IComponent)
// 3. Combining both approaches (hybrid loading)
// =============================================================================

using Baubit.DI;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleConsoleApp;


// Run all three patterns in parallel to demonstrate each approach
var task1 = ModulesLoadedFromAppsettings.RunAsync();
var task2 = ModulesLoadedFromExplicitlyGivenComponent.RunAsync();
var task3 = ModulesLoadedFromAppsettingsAndExplicitlyGivenComponent.RunAsync();

await Task.WhenAll(task1, task2, task3);

// =============================================================================
// Supporting Types
// =============================================================================

/// <summary>
/// A simple hosted service that demonstrates service injection.
/// This service receives MyService through constructor injection.
/// </summary>
class MyHostedService : IHostedService
{
    private readonly MyService myService;
    
    public MyHostedService(MyService myService)
    {
        this.myService = myService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Started with {myService.SomeStrProperty}");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// A simple service class that will be registered by modules.
/// </summary>
class MyService
{
    public string SomeStrProperty { get; init; }
    
    public MyService(string someStrProperty)
    {
        SomeStrProperty = someStrProperty;
    }
}

// =============================================================================
// Module Configuration and Module Definition
// =============================================================================

/// <summary>
/// Configuration class for MyModule.
/// Inherits from AConfiguration which allows binding from IConfiguration sections.
/// This configuration can be defined in appsettings.json or set programmatically.
/// </summary>
class MyModuleConfiguration : AConfiguration
{
    /// <summary>
    /// A string property that will be passed to MyService.
    /// </summary>
    public string? MyStringProperty { get; set; }
}

/// <summary>
/// A module that registers MyHostedService and MyService.
/// Modules encapsulate service registrations and can be composed hierarchically.
/// 
/// Key features demonstrated:
/// - Two constructor patterns (IConfiguration vs explicit configuration)
/// - Service registration in Load method
/// - Calling base.Load() to load nested modules
/// </summary>
class MyModule : AModule<MyModuleConfiguration>
{
    /// <summary>
    /// Constructor used when loading from IConfiguration (e.g., appsettings.json).
    /// The configuration section is automatically bound to MyModuleConfiguration.
    /// </summary>
    public MyModule(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Constructor used when creating the module programmatically.
    /// Accepts strongly-typed configuration and optional nested modules.
    /// </summary>
    public MyModule(MyModuleConfiguration configuration, List<IModule>? nestedModules = null) 
        : base(configuration, nestedModules)
    {
    }

    /// <summary>
    /// Register services with the DI container.
    /// IMPORTANT: Always call base.Load(services) to load nested modules.
    /// </summary>
    public override void Load(IServiceCollection services)
    {
        // Register your services
        services.AddHostedService<MyHostedService>();
        services.AddSingleton(sp => new MyService(Configuration.MyStringProperty ?? "default"));
        
        // Always call base.Load to ensure nested modules are loaded
        base.Load(services);
    }
}

// =============================================================================
// Component Definition
// =============================================================================

/// <summary>
/// A component that defines modules programmatically.
/// Components group related modules and allow code-based module composition.
/// 
/// Use components when you need to:
/// - Define modules and their configuration in code
/// - Override configuration values programmatically
/// - Combine with configuration-based module loading
/// </summary>
class MyComponent : AComponent
{
    /// <summary>
    /// Build the component by adding modules to the ComponentBuilder.
    /// This is called lazily when the component is first enumerated.
    /// </summary>
    protected override Result<ComponentBuilder> Build(ComponentBuilder componentBuilder)
    {
        // WithModule adds a module with a configuration override handler
        // The handler receives a default configuration and can modify it
        return componentBuilder.WithModule<MyModule, MyModuleConfiguration>(ConfigureModule);
    }

    /// <summary>
    /// Configure the module's configuration programmatically.
    /// This is called before the module is created.
    /// </summary>
    private void ConfigureModule(MyModuleConfiguration configuration)
    {
        configuration.MyStringProperty = "Some string value - from component code";
    }
}


