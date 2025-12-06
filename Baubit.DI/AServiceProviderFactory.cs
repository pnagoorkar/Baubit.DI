using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    public abstract class AServiceProviderFactory<TContainerBuilder> : IServiceProviderFactory<TContainerBuilder>
    {
        public Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> InternalFactory { get; }

        public List<IModule> Modules { get; } = new List<IModule>();

        public AServiceProviderFactory(Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<TContainerBuilder> internalFactory,
                                       IConfiguration configuration,
                                       IComponent[] components)
        {
            InternalFactory = internalFactory;
            Initialize(configuration, components);
        }

        private void Initialize(IConfiguration configuration, IComponent[] components)
        {
            var modules = new List<IModule>();
            if (components != null)
            {
                modules.AddRange(components.SelectMany(component => component));
            }
            LoadModules(configuration, modules);

            Modules.AddRange(modules.SelectMany(module => module.TryFlatten().ThrowIfFailed().Value));
        }

        private void LoadModules(IConfiguration configuration, List<IModule> modules)
        {
            ModuleBuilder.CreateMany(configuration)
                         .Bind(moduleBuilders => Result.Try(() => modules.AddRange(moduleBuilders.Select(moduleBuilder => moduleBuilder.Build().ThrowIfFailed().Value))));
        }

        public abstract void Load(TContainerBuilder containerBuilder);

        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(InternalFactory, Load);
            return hostApplicationBuilder;
        }
    }
}
