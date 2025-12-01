using Baubit.Configuration;
using Baubit.DI.Traceability;
using Baubit.Reflection;
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
        /// Configuration key for specifying the module type.
        /// </summary>
        public const string ModuleTypeKey = "type";

        /// <summary>
        /// Configuration key for the section containing nested module definitions.
        /// </summary>
        public const string ModulesSectionKey = "modules";

        /// <summary>
        /// Configuration key for the section containing external module source definitions.
        /// </summary>
        public const string ModuleSourcesSectionKey = "moduleSources";

        /// <summary>
        /// The type of module to be built.
        /// </summary>
        protected Type moduleType;

        /// <summary>
        /// The configuration builder used to construct the module configuration.
        /// </summary>
        protected Baubit.Configuration.ConfigurationBuilder configurationBuilder;
        
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleBuilder"/> class.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder to use.</param>
        protected ModuleBuilder(Configuration.ConfigurationBuilder configurationBuilder)
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
            return Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => Result.Try(() => new ModuleBuilder(cb)))
                .Bind(builder => builder.Initialize(configuration));
        }

        /// <summary>
        /// Initializes the builder from configuration.
        /// </summary>
        private Result<ModuleBuilder> Initialize(IConfiguration configuration)
        {
            return ResolveModuleType(configuration)
                .Bind(() => WithAdditionalConfigurationSourcesFrom<ModuleBuilder>(configuration))
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
            return Configuration.ConfigurationBuilder.CreateNew()
                .Bind(cb => cb.WithAdditionalConfigurationSources(sourceSection.Get<ConfigurationSource>()))
                .Bind(cb => cb.Build())
                .Bind(CreateNew);
        }

        private Result ResolveModuleType(IConfiguration configuration)
        {
            return TypeResolver.TryResolveType(configuration[ModuleTypeKey])
                .Bind(type => Result.Try(() => moduleType = type))
                .Bind(_ => Result.FailIf(moduleType == null, "Module type could not be resolved"));
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
        /// </remarks>
        public Result<IModule> Build()
        {
            return configurationBuilder.Build()
                .Bind(config => BuildModule<IModule>(new[] { typeof(IConfiguration) }, new object[] { config }));
        }

        /// <summary>
        /// Builds a module of the specified type using the given constructor parameters.
        /// </summary>
        protected Result<TModule> BuildModule<TModule>(Type[] paramTypes, object[] paramValues) where TModule : IModule
        {
            try
            {
                return FailIfDisposed().Bind(() => moduleType.CreateInstance<TModule>(paramTypes, paramValues));
            }
            finally
            {
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
                    moduleType = null;
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
    /// Generic builder class for creating strongly-typed <see cref="AModule{TConfiguration}"/> instances.
    /// </summary>
    /// <typeparam name="TModule">The type of module to build.</typeparam>
    /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
    public sealed class ModuleBuilder<TModule, TConfiguration> : ModuleBuilder 
        where TModule : AModule<TConfiguration> 
        where TConfiguration : AConfiguration
    {
        private readonly List<IModule> nestedModules = new List<IModule>();
        private readonly List<Action<TConfiguration>> overrideHandlers = new List<Action<TConfiguration>>();

        private ModuleBuilder(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder) : base(configurationBuilder)
        {
            moduleType = typeof(TModule);
        }

        /// <summary>
        /// Creates a new <see cref="ModuleBuilder{TModule, TConfiguration}"/> with the specified configuration builder.
        /// </summary>
        public static Result<ModuleBuilder<TModule, TConfiguration>> CreateNew(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder)
        {
            return Result.Try(() => new ModuleBuilder<TModule, TConfiguration>(configurationBuilder));
        }

        public Result<ModuleBuilder<TModule, TConfiguration>> WithOverrideHandlers(params Action<TConfiguration>[] overrideHandlers)
        {
            return Result.Try(() => this.overrideHandlers.AddRange(overrideHandlers)).Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Adds nested modules to the module being built.
        /// </summary>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModules(params IModule[] modules)
        {
            return Result.Try(() => 
            { 
                nestedModules.AddRange(modules); 
                return this; 
            });
        }

        /// <summary>
        /// Adds nested modules loaded from the specified configuration.
        /// </summary>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModulesFrom(IConfiguration configuration)
        {
            return CreateMany(configuration)
                .Bind(builders => Result.Try(() => builders.Select(b => b.Build().ThrowIfFailed().Value)))
                .Bind(modules => WithNestedModules(modules.ToArray()));
        }

        /// <summary>
        /// Adds nested modules loaded from multiple configurations.
        /// </summary>
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
        /// Builds the strongly-typed module instance.
        /// </summary>
        /// <remarks>
        /// This method disposes the builder after building. The builder cannot be reused after calling this method.
        /// </remarks>
        public new Result<TModule> Build()
        {
            return ((ConfigurationBuilder<TConfiguration>)configurationBuilder).Build()
                                                                               .Bind(CallOverrideHandlers)
                                                                               .Bind(config => BuildModule<TModule>(
                                                                                   new[] { typeof(TConfiguration), typeof(List<IModule>) }, 
                                                                                   new object[] { config, nestedModules }));
        }

        private Result<TConfiguration> CallOverrideHandlers(TConfiguration configuration)
        {
            return Result.Try(() => 
            {
                foreach(var overrideHandler in overrideHandlers)
                {
                    overrideHandler?.Invoke(configuration);
                }
            }).Bind(() => Result.Ok(configuration));
        }
    }
}
