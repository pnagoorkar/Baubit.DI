using Baubit.Configuration;
using FluentResults;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    /// <summary>
    /// Builder class for creating components that contain collections of modules.
    /// </summary>
    /// <remarks>
    /// Use <see cref="CreateNew"/> to create a new builder instance, then add modules using the
    /// <see cref="WithModule{TModule, TConfiguration}(ConfigurationBuilder{TConfiguration})"/> methods.
    /// Call <see cref="Build"/> to create the final component.
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



        private Result<ComponentBuilder> WithModule<TModule, TConfiguration>(ConfigurationBuilder<TConfiguration> configurationBuilder, params Action<TConfiguration>[] overrideHandlers) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return ModuleBuilder<TModule, TConfiguration>.CreateNew(configurationBuilder)
                                                         .Bind(mb => mb.WithOverrideHandlers(overrideHandlers))
                                                         .Bind(mb => mb.Build())
                                                         .Bind(module => Result.Try(() => modules.Add(module)))
                                                         .Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Adds a module to the component using the specified configuration builder.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="configurationBuilder">The configuration builder for the module.</param>
        /// <returns>A result containing this builder for method chaining.</returns>
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

        /// <summary>
        /// Adds a module to the component using a configuration build handler.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="configurationBuildHandler">A handler that configures the configuration builder.</param>
        /// <returns>A result containing this builder for method chaining.</returns>
        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<ConfigurationBuilder<TConfiguration>> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return WithModule<TModule, TConfiguration>(configurationBuildHandler, overrideHandlers: Array.Empty<Action<TConfiguration>>());
        }

        /// <summary>
        /// Adds a module to the component using a configuration override handler.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="configurationBuildHandler">A handler that configures the module configuration.</param>
        /// <returns>A result containing this builder for method chaining.</returns>
        public Result<ComponentBuilder> WithModule<TModule, TConfiguration>(Action<TConfiguration> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return WithModule<TModule, TConfiguration>(_ => { }, configurationBuildHandler);
        }

        /// <summary>
        /// Adds modules from existing components to this builder.
        /// </summary>
        /// <param name="components">The components whose modules should be added.</param>
        /// <returns>A result containing this builder for method chaining.</returns>
        public Result<ComponentBuilder> WithModulesFrom(params IComponent[] components)
        {
            return Result.Try(() => modules.AddRange(components.SelectMany(component => component))).Bind(() => Result.Ok(this));
        }

        /// <summary>
        /// Builds the component containing all added modules.
        /// </summary>
        /// <returns>A result containing the built component.</returns>
        public Result<IComponent> Build()
        {
            return Result.Ok<IComponent>(new ModuleCollection(modules));
        }

        /// <summary>
        /// Releases the resources used by this builder.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false if called from a finalizer.</param>
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
        /// <summary>
        /// Adds a module to the component using the specified configuration builder.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="result">The result containing the component builder.</param>
        /// <param name="configurationBuilder">The configuration builder for the module.</param>
        /// <returns>A result containing the builder for method chaining.</returns>
        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, ConfigurationBuilder<TConfiguration> configurationBuilder) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuilder));
        }

        /// <summary>
        /// Adds a module to the component using a configuration build handler.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="result">The result containing the component builder.</param>
        /// <param name="configurationBuildHandler">A handler that configures the configuration builder.</param>
        /// <returns>A result containing the builder for method chaining.</returns>
        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, Action<ConfigurationBuilder<TConfiguration>> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuildHandler));
        }

        /// <summary>
        /// Adds a module to the component using a configuration override handler.
        /// </summary>
        /// <typeparam name="TModule">The type of module to add.</typeparam>
        /// <typeparam name="TConfiguration">The type of configuration for the module.</typeparam>
        /// <param name="result">The result containing the component builder.</param>
        /// <param name="configurationBuildHandler">A handler that configures the module configuration.</param>
        /// <returns>A result containing the builder for method chaining.</returns>
        public static Result<ComponentBuilder> WithModule<TModule, TConfiguration>(this Result<ComponentBuilder> result, Action<TConfiguration> configurationBuildHandler) where TModule : AModule<TConfiguration> where TConfiguration : AConfiguration
        {
            return result.Bind(cb => cb.WithModule<TModule, TConfiguration>(configurationBuildHandler));
        }

        /// <summary>
        /// Builds the component from the builder wrapped in a result.
        /// </summary>
        /// <param name="result">The result containing the component builder.</param>
        /// <returns>A result containing the built component.</returns>
        public static Result<IComponent> Build(this Result<ComponentBuilder> result)
        {
            return result.Bind(cb => cb.Build());
        }

        /// <summary>
        /// Adds modules from existing components to the builder.
        /// </summary>
        /// <param name="result">The result containing the component builder.</param>
        /// <param name="featureFactories">The components whose modules should be added.</param>
        /// <returns>A result containing the builder for method chaining.</returns>
        public static Result<ComponentBuilder> WithModulesFrom(this Result<ComponentBuilder> result, params IComponent[] featureFactories)
        {
            return result.Bind(cb => cb.WithModulesFrom(featureFactories));
        }
    }
}
