using Baubit.Reflection;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Baubit.DI
{
    /// <summary>
    /// Extension methods for configuring host application builders with module-based dependency injection.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configuration key for specifying a custom service provider factory type.
        /// </summary>
        public const string ServiceProviderFactoryTypeKey = "serviceProviderFactoryType";

        /// <summary>
        /// Configures the host application builder to use a service provider factory loaded from configuration.
        /// </summary>
        /// <typeparam name="THostApplicationBuilder">The type of host application builder.</typeparam>
        /// <param name="hostApplicationBuilder">The host application builder to configure.</param>
        /// <param name="configuration">Optional additional configuration to add to the builder's configuration.</param>
        /// <param name="componentsFactory">Optional factory function that returns pre-built components to include.</param>
        /// <param name="onFailure">Optional callback invoked when factory creation or registration fails. Defaults to exiting the application.</param>
        /// <returns>The configured host application builder.</returns>
        /// <remarks>
        /// The factory type is resolved from the configuration key "serviceProviderFactoryType".
        /// If not specified, <see cref="ServiceProviderFactory"/> is used as the default.
        /// </remarks>
        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                           IConfiguration configuration = null,
                                                                                                           Func<IComponent[]> componentsFactory = null,
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            return hostApplicationBuilder.UseConfiguredServiceProviderFactory(configuration, componentsFactory, onFailure, null);
        }

        public static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder, TServiceProviderFactory>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                                                    IConfiguration configuration = null,
                                                                                                                                    Func<IComponent[]> componentsFactory = null,
                                                                                                                                    Action<THostApplicationBuilder, IResultBase> onFailure = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            return hostApplicationBuilder.UseConfiguredServiceProviderFactory(configuration, componentsFactory, onFailure, typeof(TServiceProviderFactory));
        }

        private static THostApplicationBuilder UseConfiguredServiceProviderFactory<THostApplicationBuilder>(this THostApplicationBuilder hostApplicationBuilder,
                                                                                                           IConfiguration configuration = null,
                                                                                                           Func<IComponent[]> componentsFactory = null,
                                                                                                           Action<THostApplicationBuilder, IResultBase> onFailure = null, 
                                                                                                           Type serviceProviderFactoryType = null) where THostApplicationBuilder : IHostApplicationBuilder
        {
            if (onFailure == null) onFailure = Exit;
            if (configuration != null) hostApplicationBuilder.Configuration.AddConfiguration(configuration);

            var factoryTypeResolutionResult = serviceProviderFactoryType == null ? TypeResolver.TryResolveType(hostApplicationBuilder.Configuration[ServiceProviderFactoryTypeKey]) : Result.Ok(serviceProviderFactoryType);
            var factoryType = factoryTypeResolutionResult.ValueOrDefault ?? typeof(ServiceProviderFactory);


            var registrationResult = factoryType.CreateInstance<IServiceProviderFactory>(new Type[] { typeof(IConfiguration), typeof(IComponent[]) },
                                                                new object[] { hostApplicationBuilder.Configuration, componentsFactory?.Invoke() }).Bind(serviceProviderFactory => serviceProviderFactory.UseConfiguredServiceProviderFactory(hostApplicationBuilder));

            if (registrationResult.IsFailed)
            {
                onFailure(hostApplicationBuilder, registrationResult.WithReasons(factoryTypeResolutionResult.Reasons));
            }

            return hostApplicationBuilder;
        }

        /// <summary>
        /// Default failure handler that prints the error and exits the application.
        /// </summary>
        /// <typeparam name="THostApplicationBuilder">The type of host application builder.</typeparam>
        /// <param name="hostApplicationBuilder">The host application builder.</param>
        /// <param name="result">The failed result containing error information.</param>
        private static void Exit<THostApplicationBuilder>(THostApplicationBuilder hostApplicationBuilder,
                                                          IResultBase result) where THostApplicationBuilder : IHostApplicationBuilder
        {
            Console.WriteLine(result.ToString());
            Environment.Exit(-1);
        }
    }
}
