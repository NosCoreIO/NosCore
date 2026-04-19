//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NosCore.EcsGenerator;

[Generator]
public class ComponentBundleGenerator : IIncrementalGenerator
{
    private const string ComponentBundleAttributeName = "NosCore.GameObject.Ecs.Attributes.ComponentBundleAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var bundleDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsPartialStruct(s),
                transform: static (ctx, _) => GetBundleInfo(ctx))
            .Where(static m => m is not null);

        var compilationAndBundles = context.CompilationProvider.Combine(bundleDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndBundles,
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsPartialStruct(SyntaxNode node)
    {
        return node is StructDeclarationSyntax sds &&
               sds.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static BundleInfo? GetBundleInfo(GeneratorSyntaxContext context)
    {
        var structDeclaration = (StructDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(structDeclaration);

        if (symbol is null)
            return null;

        var bundleAttr = symbol.GetAttributes()
            .FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == ComponentBundleAttributeName);

        if (bundleAttr is null)
            return null;

        var includedComponents = new List<IncludedComponent>();

        // Get component types from attribute constructor arguments
        foreach (var arg in bundleAttr.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Array)
            {
                foreach (var typeArg in arg.Values)
                {
                    if (typeArg.Value is INamedTypeSymbol componentSymbol)
                    {
                        var component = CreateIncludedComponent(componentSymbol);
                        if (component != null)
                            includedComponents.Add(component);
                    }
                }
            }
        }

        if (includedComponents.Count == 0)
            return null;

        return new BundleInfo(
            symbol.ContainingNamespace.ToDisplayString(),
            symbol.Name,
            includedComponents);
    }

    private static IncludedComponent? CreateIncludedComponent(INamedTypeSymbol componentSymbol)
    {
        var properties = new List<ComponentProperty>();
        var typeNamespaces = new HashSet<string>();

        foreach (var member in componentSymbol.GetMembers())
        {
            if (member is IPropertySymbol prop && prop.DeclaredAccessibility == Accessibility.Public)
            {
                var defaultValue = GetDefaultValue(prop.Type);
                properties.Add(new ComponentProperty(
                    prop.Name,
                    prop.Type.ToDisplayString(),
                    !prop.IsReadOnly,
                    defaultValue));

                CollectNamespaces(prop.Type, typeNamespaces);
            }
        }

        var componentName = componentSymbol.Name;
        var shortName = componentName.EndsWith("Component")
            ? componentName.Substring(0, componentName.Length - "Component".Length)
            : componentName;

        var fieldName = "_" + char.ToLowerInvariant(shortName[0]) + shortName.Substring(1);

        return new IncludedComponent(
            componentSymbol.ContainingNamespace.ToDisplayString(),
            componentName,
            shortName,
            fieldName,
            properties,
            typeNamespaces);
    }

    private static void CollectNamespaces(ITypeSymbol type, HashSet<string> namespaces)
    {
        if (type is INamedTypeSymbol namedType)
        {
            var ns = namedType.ContainingNamespace?.ToDisplayString();
            if (!string.IsNullOrEmpty(ns) && ns != "System" && !ns!.StartsWith("System."))
            {
                namespaces.Add(ns);
            }

            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectNamespaces(typeArg, namespaces);
            }
        }
        else if (type is IArrayTypeSymbol arrayType)
        {
            CollectNamespaces(arrayType.ElementType, namespaces);
        }
    }

    private static void Execute(Compilation compilation, ImmutableArray<BundleInfo?> bundles, SourceProductionContext context)
    {
        if (bundles.IsDefaultOrEmpty)
            return;

        var bundleDict = new Dictionary<string, BundleInfo>();
        foreach (var b in bundles)
        {
            if (b is null) continue;
            var key = $"{b.Namespace}.{b.Name}";
            if (!bundleDict.ContainsKey(key))
            {
                bundleDict[key] = b;
            }
        }

        foreach (var bundle in bundleDict.Values)
        {
            var source = GenerateBundlePartial(bundle);
            context.AddSource($"{bundle.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateBundlePartial(BundleInfo bundle)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable CS0282 // Field ordering in partial struct");
        sb.AppendLine();
        sb.AppendLine("using Arch.Core;");
        sb.AppendLine("using NosCore.GameObject.Ecs;");

        var namespaces = bundle.Components
            .Select(c => c.Namespace)
            .Concat(bundle.Components.SelectMany(c => c.TypeNamespaces))
            .Distinct()
            .Where(ns => ns != bundle.Namespace && ns != "Arch.Core" && ns != "NosCore.GameObject.Ecs")
            .OrderBy(ns => ns);

        foreach (var ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {bundle.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial struct {bundle.Name}");
        sb.AppendLine("{");

        // Generate Entity and World as readonly fields
        sb.AppendLine("    public readonly Entity Entity;");
        sb.AppendLine("    public readonly MapWorld World;");
        sb.AppendLine();

        // Generate constructor
        sb.AppendLine($"    public {bundle.Name}(Entity entity, MapWorld world)");
        sb.AppendLine("    {");
        sb.AppendLine("        Entity = entity;");
        sb.AppendLine("        World = world;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Find duplicate property names across all components
        var propertyOccurrences = new Dictionary<string, int>();
        foreach (var component in bundle.Components)
        {
            foreach (var prop in component.Properties)
            {
                if (!propertyOccurrences.ContainsKey(prop.Name))
                    propertyOccurrences[prop.Name] = 0;
                propertyOccurrences[prop.Name]++;
            }
        }
        var duplicateProperties = new HashSet<string>(
            propertyOccurrences.Where(kv => kv.Value > 1).Select(kv => kv.Key));

        // Generate implicit conversion operators
        sb.AppendLine("    // Implicit conversion operators");
        foreach (var component in bundle.Components)
        {
            sb.AppendLine($"    public static implicit operator {component.Name}({bundle.Name} bundle) => bundle.World.TryGetComponent<{component.Name}>(bundle.Entity) ?? default;");
        }
        sb.AppendLine();

        // Generate properties using casts
        foreach (var component in bundle.Components)
        {
            sb.AppendLine($"    // From {component.Name}");

            foreach (var prop in component.Properties)
            {
                var memberName = duplicateProperties.Contains(prop.Name)
                    ? $"{component.ShortName}{prop.Name}"
                    : prop.Name;

                if (prop.HasSetter)
                {
                    sb.AppendLine($"    public {prop.Type} {memberName}");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        get => (({component.Name})this).{prop.Name};");
                    sb.AppendLine($"        set => World.SetComponent(Entity, (({component.Name})this) with {{ {prop.Name} = value }});");
                    sb.AppendLine("    }");
                }
                else
                {
                    sb.AppendLine($"    public {prop.Type} {memberName} => (({component.Name})this).{prop.Name};");
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private class BundleInfo
    {
        public string Namespace { get; }
        public string Name { get; }
        public List<IncludedComponent> Components { get; }

        public BundleInfo(string @namespace, string name, List<IncludedComponent> components)
        {
            Namespace = @namespace;
            Name = name;
            Components = components;
        }
    }

    private class IncludedComponent
    {
        public string Namespace { get; }
        public string Name { get; }
        public string ShortName { get; }
        public string FieldName { get; }
        public List<ComponentProperty> Properties { get; }
        public HashSet<string> TypeNamespaces { get; }

        public IncludedComponent(string @namespace, string name, string shortName, string fieldName, List<ComponentProperty> properties, HashSet<string> typeNamespaces)
        {
            Namespace = @namespace;
            Name = name;
            ShortName = shortName;
            FieldName = fieldName;
            Properties = properties;
            TypeNamespaces = typeNamespaces;
        }
    }

    private static string GetDefaultValue(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Byte => "0",
            SpecialType.System_SByte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0",
            SpecialType.System_UInt64 => "0",
            SpecialType.System_Single => "0f",
            SpecialType.System_Double => "0d",
            SpecialType.System_String => "string.Empty",
            _ => type.IsValueType
                ? type.NullableAnnotation == NullableAnnotation.Annotated ? "null" : "default"
                : "null!"
        };
    }

    private class ComponentProperty
    {
        public string Name { get; }
        public string Type { get; }
        public bool HasSetter { get; }
        public string DefaultValue { get; }

        public ComponentProperty(string name, string type, bool hasSetter, string defaultValue)
        {
            Name = name;
            Type = type;
            HasSetter = hasSetter;
            DefaultValue = defaultValue;
        }
    }
}
