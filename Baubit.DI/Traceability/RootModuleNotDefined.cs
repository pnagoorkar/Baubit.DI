using Baubit.Traceability.Reasons;

namespace Baubit.DI.Traceability
{
    /// <summary>
    /// Reason indicating that no root module section was found in the configuration.
    /// </summary>
    /// <remarks>
    /// This reason is returned when the configuration does not contain a root module definition.
    /// </remarks>
    public class RootModuleNotDefined : AReason
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RootModuleNotDefined"/> class.
        /// </summary>
        public RootModuleNotDefined() : base("rootModule Section Not Defined !", default)
        {
        }
    }
}
