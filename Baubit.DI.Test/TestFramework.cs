[assembly: CollectionBehavior(DisableTestParallelization = false)]
[assembly: TestFramework(typeof(Baubit.DI.Test.TestFramework))]

namespace Baubit.DI.Test
{
    /// <summary>
    /// Custom test framework that registers test modules before any tests run.
    /// </summary>
    public class TestFramework : Xunit.v3.XunitTestFramework
    {
        public TestFramework()
        {
            // Register test modules with the main ModuleRegistry
            TestModuleRegistry.Register();
        }
    }
}
