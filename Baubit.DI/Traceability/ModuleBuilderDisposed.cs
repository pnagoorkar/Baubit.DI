using Baubit.Traceability.Reasons;

namespace Baubit.DI.Traceability
{
    /// <summary>
    /// Reason indicating that a <see cref="ModuleBuilder"/> has been disposed and cannot be used.
    /// </summary>
    /// <remarks>
    /// This reason is added to a failed result when attempting to build a module after the builder has been disposed.
    /// </remarks>
    public class ModuleBuilderDisposed : AReason
    {
    }
}
