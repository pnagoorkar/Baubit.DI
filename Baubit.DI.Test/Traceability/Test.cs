using Baubit.DI.Traceability;

namespace Baubit.DI.Test.Traceability
{
    /// <summary>
    /// Unit tests for traceability reason classes
    /// </summary>
    public class Test
    {
        [Fact]
        public void ModuleBuilderDisposed_CanBeInstantiated()
        {
            // Act
            var reason = new ModuleBuilderDisposed();

            // Assert
            Assert.NotNull(reason);
        }

        [Fact]
        public void ModulesNotDefined_HasCorrectMessage()
        {
            // Act
            var reason = new ModulesNotDefined();

            // Assert
            Assert.NotNull(reason);
            Assert.Equal("Modules not defined !", reason.Message);
        }

        [Fact]
        public void ModuleSourcesNotDefined_HasCorrectMessage()
        {
            // Act
            var reason = new ModuleSourcesNotDefined();

            // Assert
            Assert.NotNull(reason);
            Assert.Equal("Module sources not defined !", reason.Message);
        }

        [Fact]
        public void RootModuleNotDefined_HasCorrectMessage()
        {
            // Act
            var reason = new RootModuleNotDefined();

            // Assert
            Assert.NotNull(reason);
            Assert.Equal("rootModule Section Not Defined !", reason.Message);
        }
    }
}
