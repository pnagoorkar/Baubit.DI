# Baubit.DI

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.DI/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.DI)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.DI.svg)](https://www.nuget.org/packages/Baubit.DI/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)<br/>
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.DI/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.DI)

Modularity framework for .NET with configuration-driven module composition.

## Table of Contents

- [Installation](#installation)
- [Overview](#overview)
- [Quick Start](#quick-start)
  - [1. Define a Configuration](#1-define-a-configuration)
  - [2. Create a Module](#2-create-a-module)
- [Application Creation Patterns](#application-creation-patterns)
  - [Pattern 1: Modules from appsettings.json](#pattern-1-modules-from-appsettingsjson)
  - [Pattern 2: Modules from Code (IComponent)](#pattern-2-modules-from-code-icomponent)
  - [Pattern 3: Hybrid Loading](#pattern-3-hybrid-loading-appsettingsjson--icomponent)
- [Module Configuration in appsettings.json](#module-configuration-in-appsettingsjson)
  - [Direct Configuration](#direct-configuration)
  - [Indirect Configuration](#indirect-configuration)
  - [Hybrid Configuration](#hybrid-configuration)
- [Recursive Module Loading](#recursive-module-loading)
- [API Reference](#api-reference)
- [Configuration Keys](#configuration-keys)
- [License](#license)

## Installation

```bash
dotnet add package Baubit.DI
```

## Overview

Baubit.DI provides a modular approach to dependency injection where service registrations are encapsulated in modules. Modules can be:

- Composed hierarchically through nested modules
- Loaded dynamically from configuration
- Defined programmatically using `IComponent`
- Combined using both approaches (hybrid loading)
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
    // Constructor for loading from IConfiguration (appsettings.json)
    public MyModule(IConfiguration configuration)
        : base(configuration) { }

    // Constructor for programmatic creation
    public MyModule(MyModuleConfiguration configuration, List<IModule> nestedModules = null)
        : base(configuration, nestedModules) { }

    public override void Load(IServiceCollection services)
    {
        services.AddSingleton<IMyService>(new MyService(Configuration.ConnectionString));
        base.Load(services); // IMPORTANT: Always call to load nested modules
    }
}
```

---

## Application Creation Patterns

Baubit.DI supports three patterns for creating applications. Each pattern has its use cases.

### Pattern 1: Modules from appsettings.json

Load ALL modules from configuration. Module types, their configurations, and nested modules are defined in JSON.

```csharp
// appsettings.json defines all modules
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory()
          .Build()
          .RunAsync();
```

**appsettings.json:**
```json
{
  "modules": [
    {
      "type": "MyNamespace.MyModule, MyAssembly",
      "configuration": {
        "connectionString": "Server=localhost;Database=mydb"
      }
    }
  ]
}
```

**Use when:**
- Module configuration should be externally configurable
- You want to change behavior without recompiling
- All module settings can be expressed in configuration

---

### Pattern 2: Modules from Code (IComponent)

Load ALL modules programmatically using `IComponent`. No configuration file needed.

```csharp
// Define a component that builds modules in code
public class MyComponent : AComponent
{
    protected override Result<ComponentBuilder> Build(ComponentBuilder builder)
    {
        return builder.WithModule<MyModule, MyModuleConfiguration>(cfg =>
        {
            cfg.ConnectionString = "Server=localhost;Database=mydb";
        });
    }
}

// Load modules from component only (no appsettings.json)
await Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings())
          .UseConfiguredServiceProviderFactory(componentsFactory: () => [new MyComponent()])
          .Build()
          .RunAsync();
```

**Use when:**
- Module configuration is determined at compile time
- You need full control over module instantiation
- Configuration values come from code, not files

---

### Pattern 3: Hybrid Loading (appsettings.json + IComponent)

Combine BOTH configuration-based and code-based module loading. This is the most flexible approach.

```csharp
// Load modules from BOTH appsettings.json AND code
await Host.CreateApplicationBuilder()
          .UseConfiguredServiceProviderFactory(componentsFactory: () => [new MyComponent()])
          .Build()
          .RunAsync();
```

**Loading order:**
1. Components from `componentsFactory` are loaded first
2. Modules from appsettings.json `modules` section are loaded second

**Use when:**
- Some modules need external configuration (database connections, API keys)
- Some modules need compile-time configuration or code-based logic
- You want to extend configuration-based modules with additional code-based ones

> The full set of sample code can be found [here](./SampleConsoleApp)

---

## Module Configuration in appsettings.json

Module configurations can be defined in three ways:

### Direct Configuration

Configuration values are enclosed in a `configuration` section:

```json
{
  "modules": [
    {
      "type": "MyNamespace.MyModule, MyAssembly",
      "configuration": {
        "connectionString": "Server=localhost;Database=mydb",
        "timeout": 60,
        "modules": [
          {
            "type": "MyNamespace.NestedModule, MyAssembly",
            "configuration": { }
          }
        ]
      }
    }
  ]
}
```

### Indirect Configuration

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

**config.json:**
```json
{
  "connectionString": "Server=localhost;Database=mydb",
  "timeout": 60,
  "modules": [    
    {
      "type": "MyNamespace.NestedModule, MyAssembly",
      "configuration": {
        "somePropKey": "some_prop_value"
      }
    }
  ]
}
```

### Hybrid Configuration

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

**additional.json:**
```json
{
  "timeout": 60,
  "modules": [
    {
      "type": "MyNamespace.NestedModule, MyAssembly",
      "configuration": { }
    }
  ]
}
```

---

## Recursive Module Loading

The module system supports recursive loading - modules can contain nested modules, which can contain their own nested modules, and so on. This enables extremely modular application architectures.

**Example: Multi-level nesting**

```json
{
  "modules": [
    {
      "type": "MyNamespace.RootModule, MyAssembly",
      "configuration": {
        "modules": [
          {
            "type": "MyNamespace.FeatureModule, MyAssembly",
            "configuration": {
              "modules": [
                {
                  "type": "MyNamespace.SubFeatureModule, MyAssembly",
                  "configuration": { }
                }
              ]
            }
          }
        ]
      }
    }
  ]
}
```

When `Load(services)` is called on `RootModule`, it calls `base.Load(services)`, which iterates through `NestedModules` and calls `Load` on each, creating a recursive loading chain.

---

## API Reference

<details>
<summary><strong>IModule</strong></summary>

Interface for dependency injection modules.

| Member | Description |
|--------|-------------|
| `Configuration` | Module configuration |
| `NestedModules` | Child modules |
| `Load(IServiceCollection)` | Register services |

</details>

<details>
<summary><strong>AModule / AModule&lt;TConfiguration&gt;</strong></summary>

Abstract base classes for modules.

| Constructor | Description |
|-------------|-------------|
| `AModule(TConfiguration, List<IModule>)` | Create with config and nested modules |
| `AModule(IConfiguration)` | Create from IConfiguration section |

| Virtual Method | Description |
|----------------|-------------|
| `OnInitialized()` | Called after construction |
| `GetKnownDependencies()` | Return hardcoded module dependencies |
| `Load(IServiceCollection)` | Register services (call `base.Load` for nested modules) |

</details>

<details>
<summary><strong>IComponent / AComponent</strong></summary>

Interface and base class for grouping related modules.

| Method | Description |
|--------|-------------|
| `Build(ComponentBuilder)` | Override to configure which modules the component contains |
| `GetEnumerator()` | Returns enumerator for modules (builds on first call) |
| `Dispose()` | Release resources |

</details>

<details>
<summary><strong>ComponentBuilder</strong></summary>

Builder for creating components with collections of modules.

| Method | Description |
|--------|-------------|
| `CreateNew()` | Create a new builder instance |
| `WithModule<TModule, TConfiguration>(ConfigurationBuilder<T>)` | Add a module using configuration builder |
| `WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<T>>)` | Add a module with configuration handler |
| `WithModule<TModule, TConfiguration>(Action<T>)` | Add a module with configuration override |
| `WithModulesFrom(params IComponent[])` | Add modules from existing components |
| `Build()` | Build the component |

</details>

<details>
<summary><strong>ModuleBuilder</strong></summary>

Builds modules from configuration.

| Method | Description |
|--------|-------------|
| `CreateNew(IConfiguration)` | Create builder from config section |
| `CreateMany(IConfiguration)` | Create builders for nested modules |
| `Build()` | Build the module instance |

</details>

<details>
<summary><strong>ModuleBuilder&lt;TModule, TConfiguration&gt;</strong></summary>

Strongly-typed module builder.

| Method | Description |
|--------|-------------|
| `CreateNew(ConfigurationBuilder<TConfiguration>)` | Create typed builder |
| `WithNestedModules(params IModule[])` | Add nested modules |
| `WithNestedModulesFrom(IConfiguration)` | Load nested modules from config |
| `WithOverrideHandlers(params Action<TConfiguration>[])` | Add configuration override handlers |
| `Build()` | Build typed module |

</details>

<details>
<summary><strong>HostBuilderExtensions</strong></summary>

Extension methods for `IHostApplicationBuilder`.

| Method | Description |
|--------|-------------|
| `UseConfiguredServiceProviderFactory(IConfiguration, Func<IComponent[]>, Action<T,IResultBase>)` | Configure host with module-based DI |

</details>

<details>
<summary><strong>ServiceProviderFactory</strong></summary>

Default service provider factory that loads modules from configuration.

| Method | Description |
|--------|-------------|
| `ServiceProviderFactory(IConfiguration, IComponent[])` | Create with configuration and components |
| `ServiceProviderFactory(ServiceProviderOptions, IConfiguration, IComponent[])` | Create with options |
| `Load(IServiceCollection)` | Load all modules into services |
| `UseConfiguredServiceProviderFactory(IHostApplicationBuilder)` | Configure host builder |

</details>

<details>
<summary><strong>ModuleExtensions</strong></summary>

Extension methods for serializing modules.

| Method | Description |
|--------|-------------|
| `Serialize(JsonSerializerOptions)` | Serialize a module to JSON string |
| `SerializeAsJsonObject(JsonSerializerOptions)` | Serialize modules collection to JSON object |

</details>

---

## Configuration Keys

| Key | Description |
|-----|-------------|
| `type` | Assembly-qualified module type name |
| `configuration` | Object containing direct configuration values |
| `configurationSource` | Object specifying external configuration sources |
| `modules` | Array of nested module definitions (inside `configuration`) |
| `moduleSources` | Array of external configuration sources for modules |
| `serviceProviderFactoryType` | Custom service provider factory type (optional) |

## License

MIT
