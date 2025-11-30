using Baubit.Reflection;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;

namespace Baubit.DI
{
    public static class HostBuilderExtensions
    {
        public const string ServiceProviderFactoryTypeKey = "serviceProviderFactoryType";
        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                           IConfiguration configuration = null,
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var factoryTypeResolutionResult = TypeResolver.TryResolveType(hostApplicationBuilder.Configuration[ServiceProviderFactoryTypeKey]);
            var factoryType = factoryTypeResolutionResult.ValueOrDefault ?? typeof(ServiceProviderFactory);

            var registrationResult = InvokeFactoryConstructor(factoryType, 
                                                              new Type[] { typeof(IConfiguration) }, 
                                                              new object[] { hostApplicationBuilder.Configuration }).Bind(serviceProviderFactory => serviceProviderFactory.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

            if (registrationResult.IsFailed)
            {
                onFailure(hostApplicationBuilder, registrationResult.WithReasons(factoryTypeResolutionResult.Reasons));
            }

            return hostApplicationBuilder;
        }

        private static void Exit<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder,
                                                          IResultBase result) where THostApplicationBuilder : IHostApplicationBuilder
        {
            Console.WriteLine(result.ToString());
            Environment.Exit(-1);
        }

        private static Result<IServiceProviderFactory> InvokeFactoryConstructor(Type type, Type[] paramTypes, object[] paramValues)
        {
            return Result.Try(() =>
            {
                var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, paramTypes, null);
                return (IServiceProviderFactory)ctor.Invoke(paramValues);
            });
        }
    }
}
