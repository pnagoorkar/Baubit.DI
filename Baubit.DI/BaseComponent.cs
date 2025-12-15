using Baubit.Traceability;
using FluentResults;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    /// <summary>
    /// Abstract base class for components that build and manage a collection of modules.
    /// </summary>
    /// <remarks>
    /// Components lazily build their modules on first enumeration.
    /// Derived classes must implement <see cref="Build"/> to define which modules are included.
    /// </remarks>
    public abstract class BaseComponent : IComponent
    {
        private ComponentBuilder componentBuilder;
        private List<IModule> modules;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseComponent"/> class.
        /// </summary>
        protected BaseComponent()
        {
            componentBuilder = ComponentBuilder.CreateNew().ThrowIfFailed().Value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the modules in this component.
        /// </summary>
        /// <returns>An enumerator for the modules.</returns>
        /// <remarks>
        /// The first call to this method triggers the building of all modules.
        /// </remarks>
        public IEnumerator<IModule> GetEnumerator()
        {
            if (modules == null)
            {
                modules = Build(componentBuilder).Build().ThrowIfFailed().Value.ToList();
            }
            return modules.GetEnumerator();
        }

        /// <summary>
        /// Builds the component by adding modules to the component builder.
        /// </summary>
        /// <param name="componentBuilder">The component builder to configure.</param>
        /// <returns>A result containing the configured component builder.</returns>
        protected abstract Result<ComponentBuilder> Build(ComponentBuilder componentBuilder);

        /// <summary>
        /// Returns an enumerator that iterates through the modules in this component.
        /// </summary>
        /// <returns>An enumerator for the modules.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Releases the resources used by this component.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false if called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    componentBuilder.Dispose();
                    modules?.Clear();
                    modules = null;
                }
                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases all resources used by this component.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
