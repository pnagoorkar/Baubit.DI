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
    /// Also generates consumer-specific registries for classes marked with [GeneratedModuleRegistry].
    /// </summary>
    [Generator]
    public class ModuleRegistryGenerator : IIncrementalGenerator
    {
        private const string BaubitModuleAttributeName = "Baubit.DI.BaubitModuleAttribute";
        private const string GeneratedModuleRegistryAttributeName = "Baubit.DI.GeneratedModuleRegistryAttribute";
        private const string IModuleInterfaceName = "Baubit.DI.IModule";
        private const string IConfigurationInterfaceName = "Microsoft.Extensions.Configuration.IConfiguration";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find all classes with the BaubitModule attribute
            var moduleCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidateModuleClass(node),
                    transform: static (ctx, _) => GetSemanticModuleTarget(ctx))
                .Where(static m => m is not null);

            // Find all classes with the GeneratedModuleRegistry attribute
            var registryCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidateRegistryClass(node),
                    transform: static (ctx, _) => GetSemanticRegistryTarget(ctx))
                .Where(static r => r is not null);

            // Combine with compilation for validation
            var compilationAndModules = context.CompilationProvider.Combine(moduleCandidates.Collect());
            var registries = registryCandidates.Collect();

            // Generate the main Baubit.DI ModuleRegistry
            context.RegisterSourceOutput(compilationAndModules, (spc, source) => ExecuteMainRegistry(source.Left, source.Right!, spc));

            // Generate consumer registries
            var compilationModulesAndRegistries = compilationAndModules.Combine(registries);
            context.RegisterSourceOutput(
                compilationModulesAndRegistries,
                (spc, source) => ExecuteConsumerRegistries(source.Left.Left, source.Left.Right!, source.Right!, spc));
        }

        private static bool IsCandidateModuleClass(SyntaxNode node)
        {
            // Quick syntax-based filter: class with at least one attribute
            return node is ClassDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0;
        }

        private static bool IsCandidateRegistryClass(SyntaxNode node)
        {
            // Quick syntax-based filter: class with at least one attribute
            return node is ClassDeclarationSyntax classDecl && 
                   classDecl.AttributeLists.Count > 0 &&
                   classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        private static ModuleInfo? GetSemanticModuleTarget(GeneratorSyntaxContext context)
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

        private static RegistryInfo? GetSemanticRegistryTarget(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

            if (symbol is not INamedTypeSymbol classSymbol)
                return null;

            // Check if class has GeneratedModuleRegistryAttribute
            var attribute = classSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == GeneratedModuleRegistryAttributeName);

            if (attribute is null)
                return null;

            // Must be partial and static
            if (!classSymbol.IsStatic)
                return null;

            return new RegistryInfo(classSymbol, classDecl.GetLocation());
        }

        private static void ExecuteMainRegistry(Compilation compilation, ImmutableArray<ModuleInfo?> modules, SourceProductionContext context)
        {
            // Only generate for Baubit.DI assembly
            if (compilation.AssemblyName != "Baubit.DI")
            {
                return;
            }

            if (modules.IsDefaultOrEmpty)
            {
                // No modules found, generate empty registry
                GenerateEmptyMainRegistry(context);
                return;
            }

            var validModules = ValidateModules(compilation, modules, context);

            // Generate the registry with valid modules
            if (validModules.Count > 0)
            {
                GenerateMainRegistry(context, validModules);
            }
            else
            {
                GenerateEmptyMainRegistry(context);
            }
        }

        private static void ExecuteConsumerRegistries(
            Compilation compilation,
            ImmutableArray<ModuleInfo?> modules,
            ImmutableArray<RegistryInfo?> registries,
            SourceProductionContext context)
        {
            // Skip if this is Baubit.DI assembly (main registry is handled separately)
            if (compilation.AssemblyName == "Baubit.DI")
            {
                return;
            }

            if (registries.IsDefaultOrEmpty)
            {
                return;
            }

            var validModules = ValidateModules(compilation, modules, context);

            // Generate a registry for each consumer class marked with [GeneratedModuleRegistry]
            foreach (var registry in registries)
            {
                if (registry is null)
                    continue;

                GenerateConsumerRegistry(context, registry, validModules);
            }
        }

        private static List<ModuleInfo> ValidateModules(Compilation compilation, ImmutableArray<ModuleInfo?> modules, SourceProductionContext context)
        {
            var validModules = new List<ModuleInfo>();
            var seenKeys = new Dictionary<string, ModuleInfo>(StringComparer.OrdinalIgnoreCase);

            var imoduleSymbol = compilation.GetTypeByMetadataName(IModuleInterfaceName);
            var iconfigurationSymbol = compilation.GetTypeByMetadataName(IConfigurationInterfaceName);

            if (imoduleSymbol is null || iconfigurationSymbol is null)
            {
                // Can't validate without these types
                return validModules;
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

            return validModules;
        }

        private static bool ImplementsInterface(INamedTypeSymbol classSymbol, INamedTypeSymbol interfaceSymbol)
        {
            return classSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));
        }

        private static void GenerateMainRegistry(SourceProductionContext context, List<ModuleInfo> modules)
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
            foreach (var module in modules.OrderBy(m => m.Key, StringComparer.OrdinalIgnoreCase))
            {
                var fullTypeName = module.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"            factories[\"{EscapeString(module.Key)}\"] = cfg => new {fullTypeName}(cfg);");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("ModuleRegistry.g.cs", sb.ToString());
        }

        private static void GenerateEmptyMainRegistry(SourceProductionContext context)
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

        private static void GenerateConsumerRegistry(SourceProductionContext context, RegistryInfo registry, List<ModuleInfo> modules)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            var namespaceName = registry.ClassSymbol.ContainingNamespace.ToDisplayString();
            var className = registry.ClassSymbol.Name;

            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    {GetAccessibilityString(registry.ClassSymbol)} static partial class {className}");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Registers all modules from this assembly with the Baubit.DI ModuleRegistry.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <remarks>");
            sb.AppendLine("        /// Call this method during application startup before any module resolution occurs.");
            sb.AppendLine("        /// </remarks>");
            sb.AppendLine("        public static void Register()");
            sb.AppendLine("        {");
            sb.AppendLine("            Baubit.DI.ModuleRegistry.RegisterExternal(factories =>");
            sb.AppendLine("            {");

            // Sort modules by key for deterministic output
            foreach (var module in modules.OrderBy(m => m.Key, StringComparer.OrdinalIgnoreCase))
            {
                var fullTypeName = module.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"                factories[\"{EscapeString(module.Key)}\"] = cfg => new {fullTypeName}(cfg);");
            }

            sb.AppendLine("            });");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            var fileName = $"{registry.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.g.cs";
            fileName = fileName.Replace("<", "_").Replace(">", "_").Replace(".", "_");
            context.AddSource(fileName, sb.ToString());
        }

        private static string GetAccessibilityString(INamedTypeSymbol symbol)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                _ => "internal"
            };
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

        private class RegistryInfo
        {
            public RegistryInfo(INamedTypeSymbol classSymbol, Location location)
            {
                ClassSymbol = classSymbol;
                Location = location;
            }

            public INamedTypeSymbol ClassSymbol { get; }
            public Location Location { get; }
        }
    }
}
