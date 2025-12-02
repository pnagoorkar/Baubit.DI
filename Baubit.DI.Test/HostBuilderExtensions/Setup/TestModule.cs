using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI.Test.HostBuilderExtensions.Setup
{
    /// <summary>
    /// Test module for HostBuilderExtensions tests.
    /// </summary>
    public class TestModule : AModule<TestConfiguration>
    {
        public TestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public TestModule(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton<TestComponent>();
            base.Load(services);
        }
    }
}
