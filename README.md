# Baubit.DI

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.DI/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.DI)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.DI.svg)](https://www.nuget.org/packages/Baubit.DI/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)<br/>
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.DI/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.DI)

Modular dependency injection framework for .NET with configuration-driven module composition.

## Installation

```bash
dotnet add package Baubit.DI
```

## Overview

Baubit.DI provides a modular approach to dependency injection where service registrations are encapsulated in modules. Modules can be:

- Composed hierarchically through nested modules
- Loaded dynamically from configuration
- Integrated with `IHostApplicationBuilder` via extension methods

## Quick Start

### 1. Define a Configuration

```csharp
public class MyModuleConfiguration : AConfiguration
{
    public string ConnectionString { get; set; }
    public int Timeout { get; set; } = 30;
}
```

### 2. Create a Module

```csharp
public class MyModule : AModule<MyModuleConfiguration>
{
    public MyModule(MyModuleConfiguration configuration, List<IModule> nestedModules = null)
        : base(configuration, nestedModules) { }

    public MyModule(IConfiguration configuration)
        : base(configuration) { }

    public override void Load(IServiceCollection services)
    {
        services.AddSingleton<IMyService>(new MyService(Configuration.ConnectionString));
        base.Load(services); // Load nested modules
    }
}
```

### 3. Loading modules

The simplest way to use Baubit.DI is with `IHostApplicationBuilder`:

#### Using `HostApplicationBuilder`
```csharp
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory() // wires all modules defined in app.config
          .Build()
          .RunAsync();
```

#### Using `WebApplicationBuilder`
```csharp
var webApp = WebApplication.CreateBuilder()
                           .UseConfiguredServiceProviderFactory() // wires all modules defined in app.config
                           .Build();

// Use HTTPS, HSTS, CORS, Auth and other middleware
// Map endpoints

await webApp.RunAsync();
```

#### Using ModuleBuilder
```csharp
var module = ModuleBuilder<MyModule>.CreateNew(CreateConfigurationBuilder())
                                    .Bind(mb => mb.Build())
                                    .Value;
var services = new ServiceCollection();
module.Load(services); // Registers services defined in MyModule and its nested modules
var serviceProvider = services.BuildServiceProvider();
```

### 4. Defining modules in `appsettings.json`

Module can be defined in configuration files in 3 ways:

#### Direct definition

Configuration values are enclosed in a `configuration` section:

```json
{
  "modules": [
    {
      "type": "MyNamespace.MyModule, MyAssembly",
      "configuration": {
        "connectionString": "Server=localhost;Database=mydb",
        "timeout": 60
      }
    }
  ]
}
```

#### Indirect Configuration

Configuration is loaded from external sources via `configurationSource`:

```json
{
  "modules": [    
    {
      "type": "MyNamespace.MyModule, MyAssembly",
      "configurationSource": {
        "jsonUriStrings": ["file://path/to/config.json"]
      }
    }
  ]
}
```

config.json
```json
{
    "connectionString": "Server=localhost;Database=mydb",
    "timeout": 60,
    "modules": [    
        {
            "type": "MyNamespace.MyAnotherModule, MyAssembly",
            "configuration": {
            "somePropKey": "some_prop_value"
            }
        }
    ]
}
```
> This will load both MyModule and MyAnotherModule (as a nested module) along with their corresponding module configurations.

#### Hybrid Configuration

Combine direct values with external sources:

```json
{
  "modules": [
    {
      "type": "MyNamespace.MyModule, MyAssembly",
      "configuration": {
        "connectionString": "Server=localhost;Database=mydb"
      },
      "configurationSource": {
        "jsonUriStrings": ["file://path/to/additional.json"]
      }
    }
  ]
}
```
> `UseConfiguredServiceProviderFactory(...)` expects modules to be defined in `appsettings.json` unless overridden by explicitly passing a `configuration` to the method call.


## API Reference

### HostBuilderExtensions

Extension methods for `IHostApplicationBuilder`.

| Method | Description |
|--------|-------------|
| `UseConfiguredServiceProviderFactory(IConfiguration, Action<T,IResultBase>)` | Configure host with module-based DI |

### ServiceProviderFactory

Default service provider factory that loads modules from configuration.

| Method | Description |
|--------|-------------|
| `ServiceProviderFactory(IConfiguration)` | Create with configuration |
| `ServiceProviderFactory(ServiceProviderOptions, IConfiguration)` | Create with options |
| `Load(IServiceCollection)` | Load all modules into services |
| `UseConfiguredServiceProviderFactory(IHostApplicationBuilder)` | Configure host builder |

### IModule

Interface for dependency injection modules.

| Member | Description |
|--------|-------------|
| `Configuration` | Module configuration |
| `NestedModules` | Child modules |
| `Load(IServiceCollection)` | Register services |

### AModule / AModule&lt;TConfiguration&gt;

Abstract base classes for modules.

| Constructor | Description |
|-------------|-------------|
| `AModule(TConfiguration, List<IModule>)` | Create with config and nested modules |
| `AModule(IConfiguration)` | Create from IConfiguration section |

| Virtual Method | Description |
|----------------|-------------|
| `OnInitialized()` | Called after construction |
| `GetKnownDependencies()` | Return hardcoded module dependencies |
| `Load(IServiceCollection)` | Register services |

### ModuleBuilder

Builds modules from configuration.

| Method | Description |
|--------|-------------|
| `CreateNew(IConfiguration)` | Create builder from config section |
| `CreateMany(IConfiguration)` | Create builders for nested modules |
| `Build()` | Build the module instance |

### ModuleBuilder&lt;TModule, TConfiguration&gt;

Strongly-typed module builder.

| Method | Description |
|--------|-------------|
| `CreateNew(ConfigurationBuilder<TConfiguration>)` | Create typed builder |
| `WithNestedModules(params IModule[])` | Add nested modules |
| `WithNestedModulesFrom(IConfiguration)` | Load nested modules from config |
| `Build()` | Build typed module |

### IComponent

Interface for grouping related modules together.

| Member | Description |
|--------|-------------|
| `IEnumerable<IModule>` | Enumeration of modules in the component |
| `Dispose()` | Release component resources |

### AComponent

Abstract base class for components that lazily build modules.

| Method | Description |
|--------|-------------|
| `Build(ComponentBuilder)` | Override to configure which modules the component contains |
| `GetEnumerator()` | Returns enumerator for modules (builds on first call) |
| `Dispose()` | Release resources |

### ComponentBuilder

Builder for creating components with collections of modules.

| Method | Description |
|--------|-------------|
| `CreateNew()` | Create a new builder instance |
| `WithModule<TModule, TConfiguration>(ConfigurationBuilder<T>)` | Add a module using configuration builder |
| `WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<T>>)` | Add a module with configuration handler |
| `WithModule<TModule, TConfiguration>(Action<T>)` | Add a module with configuration override |
| `WithModulesFrom(params IComponent[])` | Add modules from existing components |
| `Build()` | Build the component |

## Configuration Keys

| Key | Description |
|-----|-------------|
| `type` | Assembly-qualified module type name |
| `configuration` | Object containing direct configuration values |
| `configurationSource` | Object specifying external configuration sources |
| `modules` | Array of nested module definitions |
| `moduleSources` | Array of external configuration sources for nested modules |
| `serviceProviderFactoryType` | Custom service provider factory type (optional) |

## License

MIT
