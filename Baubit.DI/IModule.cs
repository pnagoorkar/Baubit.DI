using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Baubit.DI
{
    public interface IModule
    {
        AConfiguration Configuration { get; }
        IReadOnlyList<IModule> NestedModules { get; }
        void Load(IServiceCollection services);
    }
}
