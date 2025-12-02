using System;
using System.Collections.Generic;

namespace Baubit.DI
{
    /// <summary>
    /// Represents a component that contains a collection of modules.
    /// </summary>
    /// <remarks>
    /// A component groups related modules together and provides enumeration over them.
    /// Components are disposable and should be disposed when no longer needed.
    /// </remarks>
    public interface IComponent : IEnumerable<IModule>, IDisposable
    {
    }
}
