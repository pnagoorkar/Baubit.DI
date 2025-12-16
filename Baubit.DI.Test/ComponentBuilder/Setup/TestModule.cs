using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Baubit.DI.Test.ComponentBuilder.Setup
{
    /// <summary>
    /// Test module class for ComponentBuilder tests.
    /// </summary>
    [BaubitModule("test-componentbuilder")]
    public class TestModule : BaseModule<TestConfiguration>
    {
        public TestModule(TestConfiguration configuration, List<IModule>? nestedModules = null)
            : base(configuration, nestedModules) { }

        public TestModule(IConfiguration configuration)
            : base(configuration) { }

        public override void Load(IServiceCollection services)
        {
            services.AddSingleton(new TestService(Configuration.Value ?? ""));
            base.Load(services);
        }
    }
}
