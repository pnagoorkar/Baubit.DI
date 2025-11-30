using FluentResults;
using Microsoft.Extensions.Hosting;

namespace Baubit.DI
{
    public interface IServiceProviderFactory
    {
        Result<THostApplicationBuilder> UseConfiguredServiceProviderFactory<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder) where THostApplicationBuilder : IHostApplicationBuilder;
    }
}
