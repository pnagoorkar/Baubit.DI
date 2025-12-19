using System;

namespace Baubit.DI
{
    /// <summary>
    /// Marks a module class for inclusion in the module registry.
    /// </summary>
    /// <remarks>
    /// Modules annotated with this attribute are discovered at compile time and registered
    /// in a generated ModuleRegistry for secure, configuration-driven module loading.
    /// The key must be unique across all modules in the compilation.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BaubitModuleAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaubitModuleAttribute"/> class.
        /// </summary>
        /// <param name="key">The unique key used to identify this module in configuration.</param>
        /// <exception cref="ArgumentException">Thrown when key is empty or whitespace.</exception>
        public BaubitModuleAttribute(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Module key cannot be empty or whitespace.", nameof(key));

            Key = key;
        }

        /// <summary>
        /// Gets the unique key used to identify this module in configuration.
        /// </summary>
        public string Key { get; }
    }
}
