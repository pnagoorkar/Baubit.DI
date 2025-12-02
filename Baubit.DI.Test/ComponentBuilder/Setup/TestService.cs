namespace Baubit.DI.Test.ComponentBuilder.Setup
{
    /// <summary>
    /// Test service class for ComponentBuilder tests.
    /// </summary>
    public class TestService
    {
        public string Value { get; }

        public TestService(string value)
        {
            Value = value;
        }
    }
}
