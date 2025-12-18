using System;

namespace Baubit.DI
{
    /// <summary>
    /// Marks a partial class to receive generated module registry methods.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute allows consumer assemblies to create their own module registries
    /// that work alongside the main Baubit.DI ModuleRegistry. This is useful for:
    /// <list type="bullet">
    /// <item>Test projects that need to register test-specific modules</item>
    /// <item>Consumer libraries that want to provide their own modules</item>
    /// <item>Plugin architectures where modules are distributed across assemblies</item>
    /// </list>
    /// </para>
    /// <para>
    /// Usage in consumer code:
    /// <code>
    /// namespace MyProject
    /// {
    ///     [GeneratedModuleRegistry]
    ///     internal static partial class MyModuleRegistry
    ///     {
    ///     }
    /// 
    ///     [BaubitModule("my-module")]
    ///     public class MyModule : Module&lt;MyConfig&gt;
    ///     {
    ///         public MyModule(IConfiguration config) : base(config) { }
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// The generator will create a partial implementation with methods to register
    /// all modules in the assembly annotated with <see cref="BaubitModuleAttribute"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class GeneratedModuleRegistryAttribute : Attribute
    {
    }
}
