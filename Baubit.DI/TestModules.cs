using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Baubit.DI.Testing
{
    /// <summary>
    /// Test configuration for unit tests.
    /// </summary>
    public class TestConfiguration : BaseConfiguration
    {
        public string? TestValue { get; set; }
    }

    /// <summary>
    /// Test module for ServiceProviderFactory tests.
    /// </summary>
    [BaubitModule("test-serviceprovider")]
    public class ServiceProviderTestModule : BaseModule<TestConfiguration>
    {
        public ServiceProviderTestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public ServiceProviderTestModule(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Load(IServiceCollection services)
        {
            // This module is for testing, it doesn't register any services by default
            base.Load(services);
        }
    }

    /// <summary>
    /// Test module for HostBuilderExtensions tests.
    /// </summary>
    [BaubitModule("test-hostbuilder")]
    public class HostBuilderTestModule : BaseModule<TestConfiguration>
    {
        public HostBuilderTestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public HostBuilderTestModule(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Load(IServiceCollection services)
        {
            // This module is for testing, it doesn't register any services by default
            base.Load(services);
        }
    }

    /// <summary>
    /// Test module for ModuleBuilder tests.
    /// </summary>
    [BaubitModule("test-modulebuilder")]
    public class ModuleBuilderTestModule : BaseModule<TestConfiguration>
    {
        public ModuleBuilderTestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public ModuleBuilderTestModule(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Load(IServiceCollection services)
        {
            // This module is for testing, it doesn't register any services by default
            base.Load(services);
        }
    }

    /// <summary>
    /// Test module for BaseModule tests.
    /// </summary>
    [BaubitModule("test-basemodule")]
    public class BaseModuleTestModule : BaseModule<TestConfiguration>
    {
        public BaseModuleTestModule(TestConfiguration configuration, List<IModule> nestedModules) : base(configuration, nestedModules)
        {
        }

        public BaseModuleTestModule(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Load(IServiceCollection services)
        {
            // This module is for testing, it doesn't register any services by default
            base.Load(services);
        }
    }
}
