using Baubit.Traceability;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    public class ServiceProviderFactory : DefaultServiceProviderFactory, IServiceProviderFactory
    {
        private List<IModule> modules = new List<IModule>();
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class
        /// with default options.
        /// </summary>
        /// <param name="configuration">Host builder configuration</param>
        public ServiceProviderFactory(IConfiguration configuration) : base()
        {
            LoadModules(configuration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class
        /// with the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The options to use for this instance.</param>
        /// <param name="configuration">Host builder configuration</param>
        public ServiceProviderFactory(ServiceProviderOptions options, IConfiguration configuration) : base(options)
        {
            LoadModules(configuration);
        }

        private void LoadModules(IConfiguration configuration)
        {
            ModuleBuilder.CreateMany(configuration)
                         .Bind(moduleBuilders => Result.Try(() => modules.AddRange(moduleBuilders.Select(moduleBuilder => moduleBuilder.Build().ThrowIfFailed().Value))));
        }

        public void Load(IServiceCollection services)
        {
            foreach (var module in modules)
            {
                module.Load(services);
            }
        }

        public Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder
        {
            hostApplicationBuilder.ConfigureContainer(this, Load);
            return hostApplicationBuilder;
        }
    }
}
