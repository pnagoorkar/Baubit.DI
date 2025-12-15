namespace Baubit.DI
{
    /// <summary>
    /// Abstract base class for module configurations.
    /// </summary>
    /// <remarks>
    /// Derive from this class to create strongly-typed configuration classes for your modules.
    /// Configuration classes are bound from <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> sections.
    /// </remarks>
    public abstract class BaseConfiguration : Baubit.Configuration.AConfiguration
    {
    }
}
