using Baubit.Configuration;
using FluentResults;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    /// <summary>
    /// Builder for creating components with collections of modules.
    /// </summary>
    /// <remarks>
    /// ComponentBuilder provides a fluent API for assembling modules into components.
    /// It supports adding modules from configuration builders, action configurators, and existing components.
    /// </remarks>
    public sealed class ComponentBuilder : IDisposable
    {
        private List<IModule> modules = new List<IModule>();
        private bool disposedValue;

        private ComponentBuilder()
        {

        }

        /// <summary>
        /// Creates a new <see cref="ComponentBuilder"/> instance.
        /// </summary>
        /// <returns>A result containing the new builder instance.</returns>
        public static Result<ComponentBuilder> CreateNew()
        {
            return new ComponentBuilder();
        }



        private Result<ComponentBuilder> WithModule<TModule, TConfiguration>(ConfigurationBuilder<TConfiguration> configurationBuilder, Func<TConfiguration, TModule> moduleFactory, params Action<TConfiguration>[] overrideHandlers) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return ModuleBuilder<TModule, TConfiguration>.CreateNew(configurationBuilder, moduleFactory)
                                                         .Bind(mb => mb.WithOverrideHandlers(overrideHandlers))
                                                         .Bind(mb => mb.Build())
                                                         .Bind(module => Result.Try(() => modules.Add(module)))
                                                         .Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Adds a module to the component using a configuration builder and module factory.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="configurationBuilder">The configuration builder to use.</param>
        /// <param name="moduleFactory">Factory function to create the module from configuration.</param>
        /// <returns>A result containing this builder instance for chaining, or failure information.</returns>
        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(ConfigurationBuilder<TConfiguration> configurationBuilder, Func<TConfiguration, TModule> moduleFactory) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return WithModule<TModule, TConfiguration>(configurationBuilder, moduleFactory, overrideHandlers: Array.Empty<Action<TConfiguration>>());
        }

        private Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<TConfiguration>> cbConfigurator, Func<TConfiguration, TModule> moduleFactory, params Action<TConfiguration>[] overrideHandlers) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return ConfigurationBuilder<TConfiguration>.CreateNew()
                                                       .Bind(cb => Result.Try(() => cbConfigurator(cb))
                                                                         .Bind(() => WithModule<TModule, TConfiguration>(cb, moduleFactory, overrideHandlers)));
        }

        /// <summary>
        /// Adds a module to the component using a configuration builder action and module factory.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="cbConfigurator">Action to configure the configuration builder.</param>
        /// <param name="moduleFactory">Factory function to create the module from configuration.</param>
        /// <returns>A result containing this builder instance for chaining, or failure information.</returns>
        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<TConfiguration>> cbConfigurator, Func<TConfiguration, TModule> moduleFactory) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return WithModule<TModule, TConfiguration>(cbConfigurator, moduleFactory, overrideHandlers: Array.Empty<Action<TConfiguration>>());
        }

        /// <summary>
        /// Adds a module to the component using a configuration action and module factory.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="configConfigurator">Action to configure the module configuration.</param>
        /// <param name="moduleFactory">Factory function to create the module from configuration.</param>
        /// <returns>A result containing this builder instance for chaining, or failure information.</returns>
        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<TConfiguration> configConfigurator, Func<TConfiguration, TModule> moduleFactory) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return WithModule<TModule, TConfiguration>(_ => { }, moduleFactory, configConfigurator);
        }

        /// <summary>
        /// Adds modules from existing components to this component.
        /// </summary>
        /// <param name="components">Array of components whose modules should be added.</param>
        /// <returns>A result containing this builder instance for chaining, or failure information.</returns>
        public Result<ComponentBuilder> WithModulesFrom(params IComponent[] components)
        {
            return Result.Try(() => modules.AddRange(components.SelectMany(component => component))).Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Builds the component with all added modules.
        /// </summary>
        /// <returns>A result containing the built component, or failure information.</returns>
        public Result<IComponent> Build()
        {
            return Result.Ok<IComponent>(new ModuleCollection(modules));
        }


        private void Dispose(bool disposing)
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

        /// <summary>
        /// Releases all resources used by this builder.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Simple implementation of <see cref="IComponent"/> that wraps a collection of modules.
    /// </summary>
    internal sealed class ModuleCollection : IComponent
    {
        private readonly List<IModule> modules;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleCollection"/> class.
        /// </summary>
        /// <param name="modules">The modules to include in the collection.</param>
        public ModuleCollection(List<IModule> modules)
        {
            this.modules = modules ?? new List<IModule>();
        }

        /// <inheritdoc/>
        public IEnumerator<IModule> GetEnumerator() => modules.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!disposedValue)
            {
                modules.Clear();
                disposedValue = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for <see cref="ComponentBuilder"/> wrapped in <see cref="Result{T}"/>.
    /// </summary>
    public static class ComponentBuilderExtensions
    {

        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, ConfigurationBuilder<TConfiguration> configurationBuilder, Func<TConfiguration, TModule> moduleFactory) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuilder, moduleFactory));
        }


        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, Action<ConfigurationBuilder<TConfiguration>> configurationBuildHandler, Func<TConfiguration, TModule> moduleFactory) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuildHandler, moduleFactory));
        }


        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, Action<TConfiguration> configurationBuildHandler, Func<TConfiguration, TModule> moduleFactory) where TModule : Module<TConfiguration> where TConfiguration : Configuration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuildHandler, moduleFactory));
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
