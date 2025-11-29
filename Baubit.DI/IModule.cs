using Baubit.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;

namespace Baubit.DI
{
    public interface IModule
    {
        AConfiguration Configuration { get; }
        IReadOnlyList<IModule> NestedModules { get; }
        void Load(IServiceCollection services);
    }
}
