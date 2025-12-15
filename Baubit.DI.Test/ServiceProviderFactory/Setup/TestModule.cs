using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.DI.Test.ServiceProviderFactory.Setup
{
    /// <summary>
    /// Test module for unit tests.
    /// </summary>
    public class TestModule : BaseModule<TestConfiguration>
    {
        public TestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public TestModule(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton<MyComponent>();
            base.Load(services);
        }
    }
}
