using Baubit.Configuration;
using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Baubit.DI
{
    public abstract class AModule : IModule
    {
        [JsonIgnore]
        public AConfiguration Configuration { get; protected set; }
        [JsonIgnore]
        public IReadOnlyList<IModule> NestedModules { get; private set; }

        public AModule(AConfiguration configuration, List<IModule> nestedModules)
        {
            Configuration = configuration;
            NestedModules = nestedModules.Concat(GetKnownDependencies()).ToList().AsReadOnly();
            OnInitialized();
        }
        /// <summary>
        /// Called by the constructor in <see cref="AModule"/> after all construction activities.
        /// Override this method to perform construction in child types.
        /// </summary>
        protected virtual void OnInitialized()
        {

        }

        /// <summary>
        /// Use this to add any know dependencies to <see cref="NestedModules"/>
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<AModule> GetKnownDependencies() => Enumerable.Empty<AModule>();

        public abstract void Load(IServiceCollection services);
    }

    public abstract class AModule<TConfiguration> : AModule where TConfiguration : AConfiguration
    {
        public new TConfiguration Configuration
        {
            get => (TConfiguration)base.Configuration;
            private set => base.Configuration = value;
        }
        protected AModule(TConfiguration configuration, List<IModule> nestedModules = null) : base(configuration, nestedModules ?? new List<IModule>())
        {

        }
        protected AModule(IConfiguration configuration) : this(configuration.Get<TConfiguration>(), LoadNestedModules(configuration))
        {

        }
        protected AModule(Configuration.ConfigurationBuilder configurationBuilder) : this(configurationBuilder.Build().ThrowIfFailed().Value)
        {

        }
        protected AModule(Configuration.ConfigurationBuilder<TConfiguration> configurationBuilder, List<IModule> nestedModules = null) : this(configurationBuilder.Build().ThrowIfFailed().Value, nestedModules)
        {
            
        }
        protected AModule(Action<ConfigurationBuilder<TConfiguration>> builderHandler, List<IModule> nestedModules = null) : 
            this(BuildConfiguration(builderHandler), nestedModules) 
        {
            
        }

        public override void Load(IServiceCollection services)
        {

        }

        private static TConfiguration BuildConfiguration(Action<ConfigurationBuilder<TConfiguration>> handler)
        {
            return ConfigurationBuilder<TConfiguration>.CreateNew()
                                                .Bind(configBuilder => Result.Try(() => handler(configBuilder)).Bind(() => configBuilder.Build()))
                                                .ThrowIfFailed()
                                                .Value;
        }

        private static List<IModule> LoadNestedModules(IConfiguration configuration)
        {
            return ModuleBuilder.CreateMany(configuration)
                                .Bind(moduleBuilders => Result.Try(() => moduleBuilders.Select(module => module.Build().ThrowIfFailed().Value).ToList()))
                                .ThrowIfFailed()
                                .Value;
        }
    }
}
