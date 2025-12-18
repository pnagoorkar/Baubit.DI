using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = false)]
[assembly: TestFramework("Baubit.DI.Test.TestFramework", "Baubit.DI.Test")]

namespace Baubit.DI.Test
{
    /// <summary>
    /// Custom test framework that registers test modules before any tests run.
    /// </summary>
    public class TestFramework : Xunit.Sdk.XunitTestFramework
    {
        public TestFramework(Xunit.Abstractions.IMessageSink messageSink)
            : base(messageSink)
        {
            // Register test modules with the main ModuleRegistry
            TestModuleRegistry.Register();
        }
    }
}
