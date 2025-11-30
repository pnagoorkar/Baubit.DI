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

### 3. Host Builder Integration

The simplest way to use Baubit.DI is with `IHostApplicationBuilder`:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.UseConfiguredServiceProviderFactory();
var host = builder.Build();
```

With external configuration:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("modules.json")
    .Build();

var builder = Host.CreateApplicationBuilder(args);
builder.UseConfiguredServiceProviderFactory(configuration);
var host = builder.Build();
```

### 4. Module Configuration

Module configurations can be defined in three ways:

#### Direct Configuration

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
  "type": "MyNamespace.MyModule, MyAssembly",
  "configurationSource": {
    "jsonUriStrings": ["file://path/to/config.json"]
  }
}
```

#### Hybrid Configuration

Combine direct values with external sources:

```json
{
  "type": "MyNamespace.MyModule, MyAssembly",
  "configuration": {
    "connectionString": "Server=localhost;Database=mydb"
  },
  "configurationSource": {
    "jsonUriStrings": ["file://path/to/additional.json"]
  }
}
```

### 5. Manual Module Loading

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var moduleBuilder = ModuleBuilder.CreateNew(configuration).Value;
var module = moduleBuilder.Build().Value;

var services = new ServiceCollection();
module.Load(services);
```

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

## Configuration Keys

| Key | Description |
|-----|-------------|
| `type` | Assembly-qualified module type name |
| `configuration` | Object containing direct configuration values |
| `configurationSource` | Object specifying external configuration sources |
| `modules` | Array of nested module definitions |
| `moduleSources` | Array of external configuration sources for nested modules |
| `serviceProviderFactoryType` | Custom service provider factory type (optional) |

## Thread Safety

All public APIs are thread-safe. Module instances are immutable after construction.

## License

MIT
