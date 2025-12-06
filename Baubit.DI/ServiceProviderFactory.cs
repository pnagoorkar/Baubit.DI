using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI
{
    public class ServiceProviderFactory : AServiceProviderFactory<IServiceCollection>
    {
        public ServiceProviderFactory(DefaultServiceProviderFactory defaultServiceProviderFactory,
                                      IConfiguration configuration,
                                      IComponent[] components) : base(new DefaultServiceProviderFactory(), configuration, components)
        {

        }
        public ServiceProviderFactory(IConfiguration configuration, IComponent[] components) : this(new DefaultServiceProviderFactory(), configuration, components)
        {
        }

        public override void Load(IServiceCollection containerBuilder)
        {
            foreach (var module in Modules)
            {
                module.Load(containerBuilder);
            }
        }
    }
}
