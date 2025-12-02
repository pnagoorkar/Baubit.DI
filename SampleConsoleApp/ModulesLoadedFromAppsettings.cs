using Microsoft.Extensions.Hosting;
using Baubit.DI;

namespace SampleConsoleApp
{
    public class ModulesLoadedFromAppsettings
    {
        public static async Task RunAsync()
        {
            await Host.CreateApplicationBuilder().UseConfiguredServiceProviderFactory().Build().RunAsync();
        }
    }
}
