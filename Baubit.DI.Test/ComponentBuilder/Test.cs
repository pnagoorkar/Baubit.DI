using Baubit.Configuration;
using Baubit.DI.Test.ComponentBuilder.Setup;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Baubit.DI.Test.ComponentBuilder
{
    /// <summary>
    /// Unit tests for <see cref="DI.ComponentBuilder"/> and <see cref="AComponent"/>.
    /// </summary>
    public class Test
    {
        #region ComponentBuilder Tests

        [Fact]
        public void CreateNew_ReturnsSuccessResult()
        {
            // Act
            var result = DI.ComponentBuilder.CreateNew();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void WithModule_ConfigurationBuilder_AddsModule()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>.CreateNew().Value;

            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>(configBuilder, cfg => new TestModule(cfg)))
                .Bind(cb => cb.Build());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public void WithModule_ConfigurationBuildHandler_AddsModule()
        {
            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<ConfigurationBuilder<TestConfiguration>>)(builder => { }), cfg => new TestModule(cfg)))
                .Bind(cb => cb.Build());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public void WithModule_ConfigurationOverrideHandler_AddsModuleWithOverride()
        {
            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "override_value"), cfg => new TestModule(cfg)))
                .Bind(cb => cb.Build());

            // Assert
            Assert.True(result.IsSuccess);
            var module = result.Value.First() as TestModule;
            Assert.NotNull(module);
            Assert.Equal("override_value", module.Configuration.Value);
        }

        [Fact]
        public void Build_ReturnsComponentWithModules()
        {
            // Arrange & Act
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test1"), cfg => new TestModule(cfg)))
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test2"), cfg => new TestModule(cfg)))
                .Bind(cb => cb.Build());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public void Dispose_DisposesBuilder()
        {
            // Arrange
            var builder = DI.ComponentBuilder.CreateNew().Value;

            // Act & Assert - should not throw
            builder.Dispose();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var builder = DI.ComponentBuilder.CreateNew().Value;

            // Act & Assert - should not throw
            builder.Dispose();
            builder.Dispose();
        }

        #endregion

        #region FeatureBuilderExtensions Tests

        [Fact]
        public void Extension_WithModule_ConfigurationBuilder_AddsModule()
        {
            // Arrange
            var configBuilder = Baubit.Configuration.ConfigurationBuilder<TestConfiguration>.CreateNew().Value;

            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .WithModule<TestModule, TestConfiguration>(configBuilder, cfg => new TestModule(cfg))
                .Build();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public void Extension_WithModule_ConfigurationBuildHandler_AddsModule()
        {
            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .WithModule<TestModule, TestConfiguration>((Action<ConfigurationBuilder<TestConfiguration>>)(builder => { }), cfg => new TestModule(cfg))
                .Build();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        [Fact]
        public void Extension_WithModule_ConfigurationOverrideHandler_AddsModule()
        {
            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test"), cfg => new TestModule(cfg))
                .Build();

            // Assert
            Assert.True(result.IsSuccess);
            var module = result.Value.First() as TestModule;
            Assert.NotNull(module);
            Assert.Equal("test", module.Configuration.Value);
        }

        #endregion

        #region AComponent Tests

        [Fact]
        public void AComponent_GetEnumerator_ReturnsModules()
        {
            // Arrange
            using var component = new TestComponent();

            // Act
            var modules = component.ToList();

            // Assert
            Assert.Equal(2, modules.Count);
            Assert.All(modules, m => Assert.IsType<TestModule>(m));
        }

        [Fact]
        public void AComponent_GetEnumerator_CalledTwice_ReturnsSameModules()
        {
            // Arrange
            using var component = new TestComponent();

            // Act
            var modules1 = component.ToList();
            var modules2 = component.ToList();

            // Assert
            Assert.Equal(modules1.Count, modules2.Count);
        }

        [Fact]
        public void AComponent_Dispose_DisposesResources()
        {
            // Arrange
            var component = new TestComponent();

            // Act - Enumerate to build modules, then dispose
            var _ = component.ToList();
            component.Dispose();

            // Assert - should complete without error
            Assert.True(true);
        }

        [Fact]
        public void AComponent_Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var component = new TestComponent();
            var _ = component.ToList();

            // Act & Assert
            component.Dispose();
            component.Dispose();
        }

        [Fact]
        public void AComponent_IEnumerable_GetEnumerator_ReturnsModules()
        {
            // Arrange
            using var component = new TestComponent();
            System.Collections.IEnumerable enumerable = component;

            // Act
            var enumerator = enumerable.GetEnumerator();
            var modules = new System.Collections.Generic.List<IModule>();
            while (enumerator.MoveNext())
            {
                modules.Add((IModule)enumerator.Current);
            }

            // Assert
            Assert.Equal(2, modules.Count);
        }

        #endregion

        #region WithModulesFrom Tests

        [Fact]
        public void WithModulesFrom_AddsModulesFromComponent()
        {
            // Arrange
            using var sourceComponent = new TestComponent();

            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModulesFrom(sourceComponent))
                .Build();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
        }

        [Fact]
        public void Extension_WithModulesFrom_AddsModulesFromComponent()
        {
            // Arrange
            using var sourceComponent = new TestComponent();

            // Act
            var result = DI.ComponentBuilder.CreateNew()
                .WithModulesFrom(sourceComponent)
                .Build();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count());
        }

        #endregion

        #region ModuleCollection Tests

        [Fact]
        public void ModuleCollection_IEnumerable_GetEnumerator_Works()
        {
            // Arrange
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test"), cfg => new TestModule(cfg)))
                .Build();

            Assert.True(result.IsSuccess);

            System.Collections.IEnumerable enumerable = result.Value;

            // Act
            var enumerator = enumerable.GetEnumerator();
            var modules = new System.Collections.Generic.List<object>();
            while (enumerator.MoveNext())
            {
                modules.Add(enumerator.Current);
            }

            // Assert
            Assert.Single(modules);
        }

        [Fact]
        public void ModuleCollection_Dispose_ClearsModules()
        {
            // Arrange
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test"), cfg => new TestModule(cfg)))
                .Build();

            Assert.True(result.IsSuccess);
            var component = result.Value;

            // Act
            component.Dispose();

            // Assert - after dispose, enumeration should return empty
            Assert.Empty(component);
        }

        [Fact]
        public void ModuleCollection_Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var result = DI.ComponentBuilder.CreateNew()
                .Bind(cb => cb.WithModule<TestModule, TestConfiguration>((Action<TestConfiguration>)(cfg => cfg.Value = "test"), cfg => new TestModule(cfg)))
                .Build();

            Assert.True(result.IsSuccess);
            var component = result.Value;

            // Act & Assert - should not throw
            component.Dispose();
            component.Dispose();
        }

        #endregion
    }
}
