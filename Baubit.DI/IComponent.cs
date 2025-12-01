using System;
using System.Collections.Generic;

namespace Baubit.DI
{
    public interface IComponent : IEnumerable<IModule>, IDisposable
    {
    }
}
