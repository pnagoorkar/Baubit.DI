using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Baubit.DI.Generators
{
    /// <summary>
    /// Incremental source generator that discovers modules annotated with [BaubitModule]
    /// and generates a ModuleRegistry for secure module loading.
    /// </summary>
    [Generator]
    public class ModuleRegistryGenerator : IIncrementalGenerator
    {
        private const string BaubitModuleAttributeName = "Baubit.DI.BaubitModuleAttribute";
        private const string IModuleInterfaceName = "Baubit.DI.IModule";
        private const string IConfigurationInterfaceName = "Microsoft.Extensions.Configuration.IConfiguration";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all classes with the BaubitModule attribute
            var moduleCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidateClass(node),
                    transform: static (ctx, _) => GetSemanticTarget(ctx))
                .Where(static m => m is not null);

            // Combine with compilation for validation
            var compilationAndModules = context.CompilationProvider.Combine(moduleCandidates.Collect());

            // Generate the source
            context.RegisterSourceOutput(compilationAndModules, (spc, source) => Execute(source.Left, source.Right!, spc));
        }

        private static bool IsCandidateClass(SyntaxNode node)
        {
            // Quick syntax-based filter: class with at least one attribute
            return node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0;
        }

        private static ModuleInfo? GetSemanticTarget(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

            if (symbol is not INamedTypeSymbol classSymbol)
                return null;

            // Check if class has BaubitModuleAttribute
            var attribute = classSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == BaubitModuleAttributeName);

            if (attribute is null)
                return null;

            // Extract the key from the attribute
            if (attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not string key)
                return null;

            return new ModuleInfo(classSymbol, key, classDecl.GetLocation());
        }

        private static void Execute(Compilation compilation, ImmutableArray<ModuleInfo?> modules, SourceProductionContext context)
        {
            if (modules.IsDefaultOrEmpty)
            {
                // No modules found, generate empty registry
                GenerateEmptyRegistry(context);
                return;
            }

            var validModules = new List<ModuleInfo>();
            var seenKeys = new Dictionary<string, ModuleInfo>(System.StringComparer.OrdinalIgnoreCase);

            var imoduleSymbol = compilation.GetTypeByMetadataName(IModuleInterfaceName);
            var iconfigurationSymbol = compilation.GetTypeByMetadataName(IConfigurationInterfaceName);

            if (imoduleSymbol is null || iconfigurationSymbol is null)
            {
                // Can't validate without these types
                GenerateEmptyRegistry(context);
                return;
            }

            foreach (var module in modules)
            {
                if (module is null)
                    continue;

                var classSymbol = module.ClassSymbol;

                // Check for duplicate keys
                if (seenKeys.TryGetValue(module.Key, out var existingModule))
                {
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "BAUBIT001",
                            "Duplicate module key",
                            "Module key '{0}' is already used by '{1}'. Each module must have a unique key.",
                            "Baubit.DI",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        module.Location,
                        module.Key,
                        existingModule.ClassSymbol.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                // Check if class implements IModule
                if (!ImplementsInterface(classSymbol, imoduleSymbol))
                {
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "BAUBIT002",
                            "Module must implement IModule",
                            "Class '{0}' must implement IModule interface to be used as a module.",
                            "Baubit.DI",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        module.Location,
                        classSymbol.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                // Check for required constructor: public ctor(IConfiguration)
                var hasValidConstructor = classSymbol.Constructors.Any(ctor =>
                    ctor.DeclaredAccessibility == Accessibility.Public &&
                    ctor.Parameters.Length == 1 &&
                    SymbolEqualityComparer.Default.Equals(ctor.Parameters[0].Type, iconfigurationSymbol));

                if (!hasValidConstructor)
                {
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "BAUBIT003",
                            "Module missing required constructor",
                            "Class '{0}' must have a public constructor accepting IConfiguration parameter.",
                            "Baubit.DI",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        module.Location,
                        classSymbol.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                // Check if class is abstract
                if (classSymbol.IsAbstract)
                {
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "BAUBIT004",
                            "Module cannot be abstract",
                            "Class '{0}' cannot be abstract. Only concrete module classes can be registered.",
                            "Baubit.DI",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        module.Location,
                        classSymbol.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                seenKeys[module.Key] = module;
                validModules.Add(module);
            }

            // Generate the registry with valid modules
            if (validModules.Count > 0)
            {
                GenerateRegistry(context, validModules);
            }
            else
            {
                GenerateEmptyRegistry(context);
            }
        }

        private static bool ImplementsInterface(INamedTypeSymbol classSymbol, INamedTypeSymbol interfaceSymbol)
        {
            return classSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));
        }

        private static void GenerateRegistry(SourceProductionContext context, List<ModuleInfo> modules)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Baubit.DI");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class ModuleRegistry");
            sb.AppendLine("    {");
            sb.AppendLine("        static partial void InitializeFactories(Dictionary<string, Func<IConfiguration, IModule>> factories)");
            sb.AppendLine("        {");

            // Sort modules by key for deterministic output
            foreach (var module in modules.OrderBy(m => m.Key, System.StringComparer.OrdinalIgnoreCase))
            {
                var fullTypeName = module.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"            factories[\"{EscapeString(module.Key)}\"] = cfg => new {fullTypeName}(cfg);");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("ModuleRegistry.g.cs", sb.ToString());
        }

        private static void GenerateEmptyRegistry(SourceProductionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Baubit.DI");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class ModuleRegistry");
            sb.AppendLine("    {");
            sb.AppendLine("        // No modules found in this assembly");
            sb.AppendLine("        static partial void InitializeFactories(Dictionary<string, Func<IConfiguration, IModule>> factories)");
            sb.AppendLine("        {");
            sb.AppendLine("            // No modules to register");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("ModuleRegistry.g.cs", sb.ToString());
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private class ModuleInfo
        {
            public ModuleInfo(INamedTypeSymbol classSymbol, string key, Location location)
            {
                ClassSymbol = classSymbol;
                Key = key;
                Location = location;
            }

            public INamedTypeSymbol ClassSymbol { get; }
            public string Key { get; }
            public Location Location { get; }
        }
    }
}
