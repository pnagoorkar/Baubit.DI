using FluentResults;
using Baubit.Configuration;

namespace Baubit.DI.Test.ComponentBuilder.Setup
{
    /// <summary>
    /// Test component class for ComponentBuilder tests.
    /// </summary>
    public class TestComponent : BaseComponent
    {
        protected override Result<Baubit.DI.ComponentBuilder> Build(Baubit.DI.ComponentBuilder featureBuilder)
        {
            return featureBuilder.WithModule<TestModule, TestConfiguration>((Action<ConfigurationBuilder<TestConfiguration>>)(cb => { }), cfg => new TestModule(cfg))
                                 .Bind(fb => fb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test"), cfg => new TestModule(cfg)));
        }
    }
}
