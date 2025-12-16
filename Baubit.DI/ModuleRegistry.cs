using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Baubit.DI
{
    /// <summary>
    /// Registry for secure module instantiation based on compile-time discovered modules.
    /// </summary>
    /// <remarks>
    /// This registry provides a secure alternative to reflection-based module loading.
    /// Only modules that were discovered at compile time and annotated with <see cref="BaubitModuleAttribute"/>
    /// can be instantiated through this registry.
    /// Thread safety: All public members are thread-safe.
    /// </remarks>
    public static partial class ModuleRegistry
    {
        /// <summary>
        /// Partial method implemented by source generator to provide module factories.
        /// </summary>
        /// <returns>Dictionary mapping module keys to factory functions.</returns>
        static partial void InitializeFactories(Dictionary<string, Func<IConfiguration, IModule>> factories);

        private static readonly Lazy<IReadOnlyDictionary<string, Func<IConfiguration, IModule>>> _factoriesLazy 
            = new Lazy<IReadOnlyDictionary<string, Func<IConfiguration, IModule>>>(() =>
            {
                var factories = new Dictionary<string, Func<IConfiguration, IModule>>(StringComparer.OrdinalIgnoreCase);
                InitializeFactories(factories);
                // Allow external registries to register their modules
                foreach (var registration in ExternalRegistrations)
                {
                    registration(factories);
                }
                return factories;
            });

        private static IReadOnlyDictionary<string, Func<IConfiguration, IModule>> Factories => _factoriesLazy.Value;

        private static readonly List<Action<Dictionary<string, Func<IConfiguration, IModule>>>> ExternalRegistrations
            = new List<Action<Dictionary<string, Func<IConfiguration, IModule>>>>();

        /// <summary>
        /// Registers an external module registry to contribute modules to the global registry.
        /// </summary>
        /// <param name="registration">Action that adds module factories to the registry.</param>
        /// <remarks>
        /// This method allows consumer assemblies with <see cref="GeneratedModuleRegistryAttribute"/>
        /// to register their modules with the main ModuleRegistry. Must be called before any
        /// module resolution occurs (typically during application startup).
        /// </remarks>
        public static void RegisterExternal(Action<Dictionary<string, Func<IConfiguration, IModule>>> registration)
        {
            if (_factoriesLazy.IsValueCreated)
            {
                throw new InvalidOperationException(
                    "External registrations must be added before any module resolution occurs. " +
                    "Call RegisterExternal during application startup before using ModuleRegistry.TryCreate.");
            }
            ExternalRegistrations.Add(registration);
        }

        /// <summary>
        /// Attempts to create a module instance from the specified key and configuration.
        /// </summary>
        /// <param name="key">The module key from configuration.</param>
        /// <param name="moduleSection">The configuration section for the module.</param>
        /// <param name="module">The created module instance, or null if the key was not found.</param>
        /// <returns>True if the module was created successfully; otherwise, false.</returns>
        /// <remarks>
        /// Key matching is case-insensitive. Only modules that were discovered at compile time
        /// and annotated with <see cref="BaubitModuleAttribute"/> can be created.
        /// </remarks>
        public static bool TryCreate(string key, IConfiguration moduleSection, out IModule module)
        {
            if (Factories.TryGetValue(key, out var factory))
            {
                module = factory(moduleSection);
                return true;
            }

            module = default!;
            return false;
        }
    }
}
