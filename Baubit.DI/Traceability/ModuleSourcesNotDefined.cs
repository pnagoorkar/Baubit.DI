using Baubit.Traceability.Reasons;

namespace Baubit.DI.Traceability
{
    /// <summary>
    /// Reason indicating that no module sources section was found in the configuration.
    /// </summary>
    /// <remarks>
    /// This reason is returned when the configuration does not contain a "moduleSources" section
    /// for defining modules from external configuration sources.
    /// </remarks>
    public class ModuleSourcesNotDefined : AReason
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleSourcesNotDefined"/> class.
        /// </summary>
        public ModuleSourcesNotDefined() : base("Module sources not defined !", default)
        {
        }
    }
}
