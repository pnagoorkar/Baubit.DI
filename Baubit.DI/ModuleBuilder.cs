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
    public class ModuleBuilder : IDisposable
    {
        public const string ModuleTypeKey = "type";
        public const string ModulesSectionKey = "modules";
        public const string ModuleSourcesSectionKey = "moduleSources";

        private Type moduleType;
        protected Baubit.Configuration.ConfigurationBuilder configurationBuilder;
        private bool disposedValue;

        protected ModuleBuilder(Configuration.ConfigurationBuilder configurationBuilder)
        {
            this.configurationBuilder = configurationBuilder;
        }

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

        protected Result<TModuleBuilder> WithAdditionaConfigurationSourcesFrom<TModuleBuilder>(params IConfiguration[] configurations) where TModuleBuilder : ModuleBuilder
        {
            return configurationBuilder.WithAdditionalConfigurationSourcesFrom(configurations).Bind(_ => Result.Ok((TModuleBuilder)this));
        }
        protected Result<TModuleBuilder> WithAdditionaConfigurationsFrom<TModuleBuilder>(params IConfiguration[] configurations) where TModuleBuilder : ModuleBuilder
        {
            return configurationBuilder.WithAdditionalConfigurationsFrom(configurations).Bind(_ => Result.Ok((TModuleBuilder)this));
        }

        public Result<IModule> Build()
        {
            return configurationBuilder.Build()
                                       .Bind(configuration => Build<IModule>(new Type[] { typeof(IConfiguration) },
                                                                             new object[] { configuration }));
        }

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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
    public sealed class ModuleBuilder<TModule, TConfiguration> : ModuleBuilder where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
    {
        private List<IModule> nestedModules = new List<IModule>();
        private ModuleBuilder(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder) : base(configurationBuilder)
        {
            
        }

        public static Result<ModuleBuilder<TModule, TConfiguration>> CreateNew(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder)
        {
            return Result.Try(() => new ModuleBuilder<TModule, TConfiguration>(configurationBuilder));
        }

        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModules(params IModule[] nestedModules)
        {
            return Result.Try(() => { this.nestedModules.AddRange(nestedModules); }).Bind(() => Result.Ok(this));
        }

        public Result<ModuleBuilder<TModule, TConfiguration>> WithNestedModulesFrom(IConfiguration configuration)
        {
            return CreateMany(configuration).Bind(moduleBuilders => Result.Try(() => moduleBuilders.Select(moduleBuilder => moduleBuilder.Build().ThrowIfFailed().Value)))
                                            .Bind(modules => WithNestedModules(modules.ToArray()));
        }

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

        public new Result<TModule> Build()
        {
            return ((ConfigurationBuilder<TConfiguration>)configurationBuilder).Build()
                                                                               .Bind(configuration => Build<TModule>(new Type[] { typeof(TConfiguration), typeof(List<IModule>) },
                                                                                                                     new object[] { configuration, nestedModules }));
        }
    }
}
