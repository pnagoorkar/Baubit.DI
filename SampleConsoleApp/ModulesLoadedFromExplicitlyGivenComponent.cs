using Microsoft.Extensions.Hosting;
using Baubit.DI;

namespace SampleConsoleApp
{
    public class ModulesLoadedFromExplicitlyGivenComponent
    {
        public static async Task RunAsync()
        {
            await Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings()).UseConfiguredServiceProviderFactory(componentsFactory: BuildComponents).Build().RunAsync();
        }

        private static IComponent[] BuildComponents()
        {
            return [new MyComponent()];
        }
    }
}
