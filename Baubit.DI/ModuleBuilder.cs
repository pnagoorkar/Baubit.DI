using Baubit.Configuration;
using Baubit.DI.Traceability;
using Baubit.Reflection;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        private Type moduleType;

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
            return Configuration.ConfigurationBuilder
                                .CreateNew()
                                .Bind(cb => Result.Try(() => new ModuleBuilder(cb)))
                                .Bind(moduleBuilder => moduleBuilder.DetermineModuleType(configuration)
                                                                    .Bind(() => moduleBuilder.WithAdditionaConfigurationSourcesFrom<ModuleBuilder>(configuration))
                                                                    .Bind(_ => moduleBuilder.WithAdditionaConfigurationsFrom<ModuleBuilder>(configuration))
                                                                    .Bind(_ => Result.Ok(moduleBuilder)));
        }

        /// <summary>
        /// Creates multiple <see cref="ModuleBuilder"/> instances from nested module definitions in the configuration.
        /// </summary>
        /// <param name="configuration">The configuration section containing module definitions.</param>
        /// <returns>A result containing the collection of module builders, or failure information.</returns>
        public static Result<IEnumerable<ModuleBuilder>> CreateMany(IConfiguration configuration)
        {
            try
            {
                var moduleBuilders = new List<ModuleBuilder>();
                GetDirectlyDefinedModuleSections(configuration).Bind(moduleSections => Result.Try(() =>
                {
                    foreach (var moduleSection in moduleSections)
                    {
                        var moduleBuilder = CreateNew(moduleSection).ThrowIfFailed().Value;
                        moduleBuilders.Add(moduleBuilder);
                    }
                })).ThrowIfFailed();

                GetInDirectlyDefinedModuleSections(configuration).Bind(sourcesSections => Result.Try(() => 
                {
                    foreach(var sourcesSection in sourcesSections)
                    {
                        var moduleBuilder = Configuration.ConfigurationBuilder
                                                         .CreateNew()
                                                         .Bind(cb => cb.WithAdditionalConfigurationSources(sourcesSection.Get<ConfigurationSource>()))
                                                         .Bind(cb => cb.Build())
                                                         .Bind(cfg => CreateNew(cfg))
                                                         .ThrowIfFailed()
                                                         .Value;
                        moduleBuilders.Add(moduleBuilder);
                    }
                })).ThrowIfFailed();

                return Result.Ok<IEnumerable<ModuleBuilder>>(moduleBuilders);
            }
            catch(Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        private Result DetermineModuleType(IConfiguration configuration)
        {
            return TypeResolver.TryResolveType(configuration[ModuleTypeKey])
                               .Bind(type => Result.Try(() => moduleType = type))
                               .Bind(_ => Result.FailIf(moduleType == null, string.Empty));
        }

        /// <summary>
        /// Adds additional configuration sources from the specified configurations.
        /// </summary>
        /// <typeparam name="TModuleBuilder">The type of module builder.</typeparam>
        /// <param name="configurations">The configurations to extract sources from.</param>
        /// <returns>A result containing this builder instance, or failure information.</returns>
        protected Result<TModuleBuilder> WithAdditionaConfigurationSourcesFrom<TModuleBuilder>(params IConfiguration[] configurations) where TModuleBuilder : ModuleBuilder
        {
            return configurationBuilder.WithAdditionalConfigurationSourcesFrom(configurations).Bind(_ => Result.Ok((TModuleBuilder)this));
        }

        /// <summary>
        /// Adds additional configurations from the specified configuration sections.
        /// </summary>
        /// <typeparam name="TModuleBuilder">The type of module builder.</typeparam>
        /// <param name="configurations">The configurations to add.</param>
        /// <returns>A result containing this builder instance, or failure information.</returns>
        protected Result<TModuleBuilder> WithAdditionaConfigurationsFrom<TModuleBuilder>(params IConfiguration[] configurations) where TModuleBuilder : ModuleBuilder
        {
            return configurationBuilder.WithAdditionalConfigurationsFrom(configurations).Bind(_ => Result.Ok((TModuleBuilder)this));
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
                                       .Bind(configuration => Build<IModule>(new Type[] { typeof(IConfiguration) },
                                                                             new object[] { configuration }));
        }

        /// <summary>
        /// Builds a module of the specified type using the given constructor parameters.
        /// </summary>
        /// <typeparam name="TModule">The type of module to build.</typeparam>
        /// <param name="paramsTypeFilter">The types of the constructor parameters.</param>
        /// <param name="ctorParams">The constructor parameter values.</param>
        /// <returns>A result containing the built module, or failure information.</returns>
        protected Result<TModule> Build<TModule>(Type[] paramsTypeFilter, object[] ctorParams) where TModule : IModule
        {
            try
            {
                return FailIfDisposed().Bind(() => Result.Try(() =>
                {
                    return (TModule)moduleType.GetConstructor(BindingFlags.Instance | BindingFlags.Public,
                                                              default,
                                                              paramsTypeFilter,
                                                              default)
                                              .Invoke(ctorParams);
                }));
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Checks if the builder has been disposed and returns a failed result if so.
        /// </summary>
        /// <returns>
        /// A <see cref="Result"/> that is successful if the builder is not disposed;
        /// otherwise, a failed result with <see cref="ModuleBuilderDisposed"/> reason.
        /// </returns>
        private Result FailIfDisposed()
        {
            return Result.FailIf(disposedValue, new Error(string.Empty))
                         .AddReasonIfFailed(new ModuleBuilderDisposed());
        }

        #region DirectlyDefinedNestedModules

        private static Result<IEnumerable<IConfiguration>> GetDirectlyDefinedModuleSections(IConfiguration configuration)
        {
            return GetModulesSectionOrDefault(configuration).Bind(modulesSection => Result.Try(() => modulesSection?.GetChildren().Cast<IConfiguration>() ?? new List<IConfiguration>()));
        }

        private static Result<IConfigurationSection> GetModulesSectionOrDefault(IConfiguration configuration)
        {
            return Result.Ok(GetModulesSection(configuration).ValueOrDefault);
        }

        private static Result<IConfigurationSection> GetModulesSection(IConfiguration configurationSection)
        {
            var modulesSection = configurationSection.GetSection(ModulesSectionKey);
            return modulesSection.Exists() ?
                   Result.Ok(modulesSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModulesNotDefined());
        }
        #endregion

        #region IndirectlyDefinedNestedModules

        private static Result<IEnumerable<IConfiguration>> GetInDirectlyDefinedModuleSections(IConfiguration configuration)
        {
            return GetModuleSourcesSectionOrDefault(configuration).Bind(moduleSourcesSection => Result.Try(() => moduleSourcesSection?.GetChildren().Cast<IConfiguration>() ?? new List<IConfiguration>()));
        }

        /// <summary>
        /// Gets the module sources section from the configuration, or null if not defined.
        /// </summary>
        /// <param name="configuration">The configuration to search.</param>
        /// <returns>A result containing the module sources section, or null if not found.</returns>
        public static Result<IConfigurationSection> GetModuleSourcesSectionOrDefault(IConfiguration configuration)
        {
            return Result.Ok(GetModuleSourcesSection(configuration).ValueOrDefault);
        }

        private static Result<IConfigurationSection> GetModuleSourcesSection(IConfiguration configurationSection)
        {
            var moduleSourcesSection = configurationSection.GetSection(ModuleSourcesSectionKey);
            return moduleSourcesSection.Exists() ?
                   Result.Ok(moduleSourcesSection) :
                   Result.Fail(Enumerable.Empty<IError>()).WithReason(new ModuleSourcesNotDefined());
        }

        /// <summary>
        /// Releases the resources used by the module builder.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources; otherwise, false.</param>
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
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
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
    public sealed class ModuleBuilder<TModule, TConfiguration> : ModuleBuilder where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
    {
        private List<IModule> nestedModules = new List<IModule>();

        private ModuleBuilder(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder) : base(configurationBuilder)
        {
            
        }

        /// <summary>
        /// Creates a new <see cref="ModuleBuilder{TModule, TConfiguration}"/> with the specified configuration builder.
        /// </summary>
        /// <param name="configurationBuilder">The typed configuration builder to use.</param>
        /// <returns>A result containing the module builder, or failure information.</returns>
        public static Result<ModuleBuilder<TModule, TConfiguration>> CreateNew(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder)
        {
            return Result.Try(() => new ModuleBuilder<TModule, TConfiguration>(configurationBuilder));
        }

        /// <summary>
        /// Adds nested modules to the module being built.
        /// </summary>
        /// <param name="nestedModules">The modules to add as nested dependencies.</param>
        /// <returns>A result containing this builder instance, or failure information.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModules(params IModule[] nestedModules)
        {
            return Result.Try(() => { this.nestedModules.AddRange(nestedModules); }).Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Adds nested modules loaded from the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration containing nested module definitions.</param>
        /// <returns>A result containing this builder instance, or failure information.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModulesFrom(IConfiguration configuration)
        {
            return CreateMany(configuration).Bind(moduleBuilders => Result.Try(() => moduleBuilders.Select(moduleBuilder => moduleBuilder.Build().ThrowIfFailed().Value)))
                                            .Bind(modules => WithNestedModules(modules.ToArray()));
        }

        /// <summary>
        /// Adds nested modules loaded from multiple configurations.
        /// </summary>
        /// <param name="configurations">The configurations containing nested module definitions.</param>
        /// <returns>A result containing this builder instance, or failure information.</returns>
        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModulesFrom(params IConfiguration[] configurations)
        {
            try
            {
                foreach (var configuration in configurations)
                {
                    WithNestedModulesFrom(configuration).ThrowIfFailed();
                }
                return Result.Ok(this);
            }
            catch (Exception exp)
            {
                return Result.Fail(new ExceptionalError(exp));
            }
        }

        /// <summary>
        /// Builds the strongly-typed module instance.
        /// </summary>
        /// <returns>A result containing the built module, or failure information.</returns>
        /// <remarks>
        /// This method disposes the builder after building. The builder cannot be reused after calling this method.
        /// </remarks>
        public new Result<TModule> Build()
        {
            return ((ConfigurationBuilder<TConfiguration>)configurationBuilder).Build()
                                                                               .Bind(configuration => Build<TModule>(new Type[] { typeof(TConfiguration), typeof(List<IModule>) },
                                                                                                                     new object[] { configuration, nestedModules }));
        }
    }
}
