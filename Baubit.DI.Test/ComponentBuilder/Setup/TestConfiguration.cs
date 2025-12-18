using Baubit.Configuration;

namespace Baubit.DI.Test.ComponentBuilder.Setup
{
    /// <summary>
    /// Test configuration class for ComponentBuilder tests.
    /// </summary>
    public class TestConfiguration : Configuration
    {
        public string? Value { get; set; }
    }
}
