using Baubit.Configuration;
using Baubit.DI.Traceability;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    /// <summary>
    /// Builder class for creating <see cref="IModule"/> instances from configuration.
    /// </summary>
    /// <remarks>
    /// This builder supports creating modules from configuration sections that define the module type,
    /// nested modules, and module sources. It implements <see cref="IDisposable"/> to properly clean up resources.
    /// </remarks>
    public class ModuleBuilder : IDisposable
    {
        /// <summary>
        /// Configuration key for specifying the module key.
        /// </summary>
        public const string ModuleKey = "key";

        /// <summary>
        /// Configuration key for the section containing nested module definitions.
        /// </summary>
        public const string ModulesSectionKey = "modules";

        /// <summary>
        /// Configuration key for the section containing external module source definitions.
        /// </summary>
        public const string ModuleSourcesSectionKey = "moduleSources";

        protected string moduleKeyValue;

        /// <summary>
        /// The configuration builder used to construct the module configuration.
        /// </summary>
        protected Baubit.Configuration.ConfigurationBuilder configurationBuilder;

        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleBuilder"/> class.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder to use.</param>
        protected ModuleBuilder(Baubit.Configuration.ConfigurationBuilder configurationBuilder)
        {
            this.configurationBuilder = configurationBuilder;
        }

        /// <summary>
        /// Creates a new <see cref="ModuleBuilder"/> from the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration section containing module definition.</param>
        /// <returns>A result containing the module builder, or failure information.</returns>
        public static Result<ModuleBuilder> CreateNew(IConfiguration configuration)
        {
            return Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => Result.Try(() => new ModuleBuilder(cb)))
                .Bind(builder => builder.Initialize(configuration));
        }

        /// <summary>
        /// Initializes the builder from configuration.
        /// </summary>
        private Result<ModuleBuilder> Initialize(IConfiguration configuration)
        {
            return Result.Try(() =>
            {
                moduleKeyValue = configuration[ModuleKey];
                if (string.IsNullOrWhiteSpace(moduleKeyValue))
                {
                    throw new InvalidOperationException($"Module key '{ModuleKey}' is required but was not specified or is empty. Ensure the configuration contains a '{ModuleKey}' key with a valid module identifier.");
                }
                return moduleKeyValue;
            })
            .Bind(_ => WithAdditionalConfigurationSourcesFrom<ModuleBuilder>(configuration))
            .Bind(_ => WithAdditionalConfigurationsFrom<ModuleBuilder>(configuration))
            .Bind(_ => Result.Ok(this));
        }

        /// <summary>
        /// Creates multiple <see cref="ModuleBuilder"/> instances from nested module definitions in the configuration.
        /// </summary>
        /// <param name="configuration">The configuration section containing module definitions.</param>
        /// <returns>A result containing the collection of module builders, or failure information.</returns>
        public static Result<IEnumerable<ModuleBuilder>> CreateMany(IConfiguration configuration)
        {
            return Result.Try(() =>
            {
                var builders = new List<ModuleBuilder>();
                builders.AddRange(CreateBuildersFromDirectModules(configuration));
                builders.AddRange(CreateBuildersFromModuleSources(configuration));
                return builders.AsEnumerable();
            });
        }

        private static IEnumerable<ModuleBuilder> CreateBuildersFromDirectModules(IConfiguration configuration)
        {
            var sections = GetModulesSectionOrDefault(configuration).ValueOrDefault?.GetChildren() ?? Enumerable.Empty<IConfigurationSection>();
            return sections.Select(section => CreateNew(section).ThrowIfFailed().Value);
        }

        private static IEnumerable<ModuleBuilder> CreateBuildersFromModuleSources(IConfiguration configuration)
        {
            var sections = GetModuleSourcesSectionOrDefault(configuration).ValueOrDefault?.GetChildren() ?? Enumerable.Empty<IConfigurationSection>();
            return sections.Select(section => CreateBuilderFromSource(section).ThrowIfFailed().Value);
        }

        private static Result<ModuleBuilder> CreateBuilderFromSource(IConfigurationSection sourceSection)
        {
            return Baubit.Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithAdditionalConfigurationSources(sourceSection.Get<ConfigurationSource>()))
                .Bind(cb => cb.Build())
                .Bind(CreateNew);
        }

        /// <summary>
        /// Adds additional configuration sources from the specified configurations.
        /// </summary>
        protected Result<TModuleBuilder> WithAdditionalConfigurationSourcesFrom<TModuleBuilder>(params IConfiguration[] configurations)
            where TModuleBuilder : ModuleBuilder
        {
            return configurationBuilder.WithAdditionalConfigurationSourcesFrom(configurations)
                .Bind(_ => Result.Ok((TModuleBuilder)this));
        }

        /// <summary>
        /// Adds additional configurations from the specified configuration sections.
        /// </summary>
        protected Result<TModuleBuilder> WithAdditionalConfigurationsFrom<TModuleBuilder>(params IConfiguration[] configurations)
            where TModuleBuilder : ModuleBuilder
        {
            return configurationBuilder.WithAdditionalConfigurationsFrom(configurations)
                .Bind(_ => Result.Ok((TModuleBuilder)this));
        }

        /// <summary>
        /// Builds an <see cref="IModule"/> instance from the configured settings.
        /// </summary>
        /// <returns>A result containing the built module, or failure information.</returns>
        /// <remarks>
        /// This method disposes the builder after building. The builder cannot be reused after calling this method.
        /// Modules must be registered with [BaubitModule] attribute to be loaded.
        /// </remarks>
        public Result<IModule> Build()
        {
            try
            {
                return FailIfDisposed().Bind(() => configurationBuilder.Build()
                                       .Bind(config =>
                                       {
                                           if (ModuleRegistry.TryCreate(moduleKeyValue, config, out var module))
                                           {
                                               return Result.Ok(module);
                                           }

                                           return Result.Fail<IModule>($"Unknown module key '{moduleKeyValue}'. Ensure the module is annotated with [BaubitModule(\"{moduleKeyValue}\")] attribute.");
                                       }));
            }
            finally
            {
                // Always dispose to prevent reuse
                Dispose();
            }
        }

        private Result FailIfDisposed()
        {
            return Result.FailIf(disposedValue, new Error("Builder has been disposed"))
                .AddReasonIfFailed(new ModuleBuilderDisposed());
        }

        #region Configuration Section Helpers

        /// <summary>
        /// Gets the module sources section from the configuration, or null if not defined.
        /// </summary>
        public static Result<IConfigurationSection> GetModuleSourcesSectionOrDefault(IConfiguration configuration)
        {
            return Result.Ok(GetModuleSourcesSection(configuration).ValueOrDefault);
        }

        private static Result<IConfigurationSection> GetModulesSectionOrDefault(IConfiguration configuration)
        {
            return Result.Ok(GetModulesSection(configuration).ValueOrDefault);
        }

        private static Result<IConfigurationSection> GetModulesSection(IConfiguration configuration)
        {
            var section = configuration.GetSection(ModulesSectionKey);
            return section.Exists()
                ? Result.Ok(section)
                : Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModulesNotDefined());
        }

        private static Result<IConfigurationSection> GetModuleSourcesSection(IConfiguration configuration)
        {
            var section = configuration.GetSection(ModuleSourcesSectionKey);
            return section.Exists()
                ? Result.Ok(section)
                : Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModuleSourcesNotDefined());
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases the resources used by the module builder.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    configurationBuilder?.Dispose();
                    configurationBuilder = null;
                }
                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the module builder.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// Generic builder class for creating strongly-typed <see cref="Module{TConfiguration}"/> instances.
    /// </summary>
    /// <typeparam name="TModule">The type of module to build.</typeparam>
    /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
    public sealed class ModuleBuilder<TModule, TConfiguration> : ModuleBuilder
        where TModule : Module<TConfiguration>
        where TConfiguration : Configuration
    {
        private Func<TConfiguration, TModule> moduleFactory = null;
        private readonly List<IModule> nestedModules = new List<IModule>();
        private readonly List<Action<TConfiguration>> overrideHandlers = new List<Action<TConfiguration>>();

        private ModuleBuilder(Baubit.Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder, Func<TConfiguration, TModule> moduleFactory) : base(configurationBuilder)
        {
            this.moduleFactory = moduleFactory;
        }

        /// <summary>
        /// Creates a new <see cref="ModuleBuilder{TModule, TConfiguration}"/> instance.
        /// </summary>
        /// <param name="configurationBuilder">The typed configuration builder to use.</param>
        /// <param name="moduleFactory">A factory function that creates a module from its configuration.</param>
        /// <returns>A result containing the new builder instance.</returns>
        public static Result<ModuleBuilder<TModule, TConfiguration>> CreateNew(Baubit.Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder,
                                                                               Func<TConfiguration, TModule> moduleFactory)
        {
            return Result.Try(() => new ModuleBuilder<TModule, TConfiguration>(configurationBuilder, moduleFactory));
        }

        /// <summary>
        /// Adds configuration override handlers that are applied after the configuration is built.
        /// </summary>
        /// <param name="overrideHandlers">Actions that mutate the configuration after it is built.</param>
        /// <returns>A result containing this builder for chaining.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithOverrideHandlers(params Action<TConfiguration>[] overrideHandlers)
        {
            return Result.Try(() => this.overrideHandlers.AddRange(overrideHandlers)).Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Adds the specified modules as nested modules of the module being built.
        /// </summary>
        /// <param name="modules">The modules to add as nested modules.</param>
        /// <returns>A result containing this builder for chaining.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModules(params IModule[] modules)
        {
            return Result.Try(() =>
            {
                nestedModules.AddRange(modules);
                return this;
            });
        }

        /// <summary>
        /// Loads and adds nested modules from the specified configuration section.
        /// </summary>
        /// <param name="configuration">The configuration section containing module definitions.</param>
        /// <returns>A result containing this builder for chaining, or failure information.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModulesFrom(IConfiguration configuration)
        {
            return CreateMany(configuration)
                .Bind(builders => Result.Try(() => builders.Select(b => b.Build().ThrowIfFailed().Value)))
                .Bind(modules => WithNestedModules(modules.ToArray()));
        }

        /// <summary>
        /// Loads and adds nested modules from all specified configuration sections.
        /// </summary>
        /// <param name="configurations">The configuration sections to load nested modules from.</param>
        /// <returns>A result containing this builder for chaining, or failure information.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModulesFrom(params IConfiguration[] configurations)
        {
            return Result.Try(() =>
            {
                foreach (var config in configurations)
                {
                    WithNestedModulesFrom(config).ThrowIfFailed();
                }
                return this;
            });
        }

        /// <summary>
        /// Builds the strongly-typed module instance from the configured settings.
        /// </summary>
        /// <returns>A result containing the built module, or failure information.</returns>
        /// <remarks>
        /// This method disposes the builder after building. The builder cannot be reused after calling this method.
        /// Override handlers are applied to the configuration before the module factory is invoked.
        /// </remarks>
        public new Result<TModule> Build()
        {
            try
            {
                return ((ConfigurationBuilder<TConfiguration>)configurationBuilder).Build()
                                                                                   .Bind(CallOverrideHandlers)
                                                                                   .Bind(config => Result.Try(() => moduleFactory?.Invoke(config)));
            }
            finally
            {
                Dispose();
            }
        }

        private Result<TConfiguration> CallOverrideHandlers(TConfiguration configuration)
        {
            return Result.Try(() =>
            {
                foreach (var overrideHandler in overrideHandlers)
                {
                    overrideHandler?.Invoke(configuration);
                }
            }).Bind(() => Result.Ok(configuration));
        }
    }
}
