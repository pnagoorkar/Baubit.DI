using Baubit.Traceability.Reasons;

namespace Baubit.DI.Traceability
{
    /// <summary>
    /// Reason indicating that no modules section was found in the configuration.
    /// </summary>
    /// <remarks>
    /// This reason is returned when the configuration does not contain a "modules" section
    /// for defining nested modules inline.
    /// </remarks>
    public class ModulesNotDefined : AReason
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModulesNotDefined"/> class.
        /// </summary>
        public ModulesNotDefined() : base("Modules not defined !", default)
        {
        }
    }
}
