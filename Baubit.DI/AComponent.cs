using Baubit.Traceability;
using FluentResults;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Baubit.DI
{
    public abstract class AComponent : IComponent
    {
        private ComponentBuilder componentBuilder;
        private List<IModule> modules;
        private bool disposedValue;

        protected AComponent()
        {
            componentBuilder = ComponentBuilder.CreateNew().ThrowIfFailed().Value;
        }

        public IEnumerator<IModule> GetEnumerator()
        {
            if (modules == null)
            {
                modules = Build(componentBuilder).Build().ThrowIfFailed().Value.ToList();
            }
            return modules.GetEnumerator();
        }

        protected abstract Result<ComponentBuilder> Build(ComponentBuilder featureBuilder);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
