using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI.Test.HostBuilderExtensions.Setup
{
    /// <summary>
    /// Custom service provider factory for testing custom factory type resolution.
    /// </summary>
    public class CustomServiceProviderFactory : Baubit.DI.ServiceProviderFactory
    {
        public static bool WasCreated { get; private set; }

        public CustomServiceProviderFactory(IConfiguration configuration, IComponent[] components) : base(configuration, components)
        {
            WasCreated = true;
        }

        public static void Reset()
        {
            WasCreated = false;
        }
    }
}
