using Baubit.Configuration;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    public class ComponentBuilder : IDisposable
    {
        private List<IModule> modules = new List<IModule>();
        private bool disposedValue;

        private ComponentBuilder()
        {

        }

        public static Result<ComponentBuilder> CreateNew()
        {
            return new ComponentBuilder();
        }



        private Result<ComponentBuilder> WithModule<TModule, TConfiguration>(ConfigurationBuilder<TConfiguration> configurationBuilder, params Action<TConfiguration>[] overrideHandlers) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return ModuleBuilder<TModule, TConfiguration>.CreateNew(configurationBuilder)
                                                         .Bind(mb => mb.WithOverrideHandlers(overrideHandlers))
                                                         .Bind(mb => mb.Build())
                                                         .Bind(module => Result.Try(() => modules.Add(module)))
                                                         .Bind(() => Result.Ok(this));
        }

        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(ConfigurationBuilder<TConfiguration> configurationBuilder) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return WithModule<TModule, TConfiguration>(configurationBuilder, overrideHandlers: Array.Empty<Action<TConfiguration>>());
        }

        private Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<TConfiguration>> configurationBuildHandler, params Action<TConfiguration>[] overrideHandlers) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return ConfigurationBuilder<TConfiguration>.CreateNew()
                                                       .Bind(cb => Result.Try(() => configurationBuildHandler(cb))
                                                                         .Bind(() => WithModule<TModule, TConfiguration>(cb, overrideHandlers)));
        }

        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<TConfiguration>> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return WithModule<TModule, TConfiguration>(configurationBuildHandler, overrideHandlers: Array.Empty<Action<TConfiguration>>());
        }
        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<TConfiguration> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return WithModule<TModule, TConfiguration>(_ => { }, configurationBuildHandler);
        }
        public Result<ComponentBuilder> WithModulesFrom(params IComponent[] components)
        {
            return Result.Try(() => modules.AddRange(components.SelectMany(component => component))).Bind(() => Result.Ok(this));
        }
        public Result<IComponent> Build()
        {
            return Result.Ok((IComponent)modules.AsEnumerable());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
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
    }

    public static class FeatureBuilderExtensions
    {
        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, ConfigurationBuilder<TConfiguration> configurationBuilder) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuilder));
        }

        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, Action<ConfigurationBuilder<TConfiguration>> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuildHandler));
        }

        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, Action<TConfiguration> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuildHandler));
        }
        public static Result<IComponent> Build(this Result<ComponentBuilder> result)
        {
            return result.Bind(cb => cb.Build());
        }
        public static Result<ComponentBuilder> WithModulesFrom(this Result<ComponentBuilder> result, params IComponent[] featureFactories)
        {
            return result.Bind(cb => cb.WithModulesFrom(featureFactories));
        }
    }
}
