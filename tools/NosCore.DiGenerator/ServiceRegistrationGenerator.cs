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
using Microsoft.CodeAnalysis.Text;

namespace NosCore.DiGenerator;

[Generator]
public class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private const string SingletonMarker = "NosCore.GameObject.Infastructure.ISingletonService";
    private const string NrunEventHandler = "NosCore.GameObject.Messaging.Handlers.Nrun.INrunEventHandler";
    private const string QuestTypeHandler = "NosCore.GameObject.Services.QuestService.IQuestTypeHandler";
    private const string HubClientSuffix = "HubClient";

    private static readonly string[] ConventionSuffixes =
        { "Service", "Provider", "Resolver", "Calculator", "Catalog", "Queue", "Ai" };

    private static readonly DiagnosticDescriptor DeadSingletonMarker = new(
        "NOSDI001",
        "ISingletonService marker has no effect",
        "'{0}' implements ISingletonService but its name matches none of the registration suffixes ({1}), so it is never registered and the marker is dead",
        "NosCore.DependencyInjection",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnregisterableOpenGeneric = new(
        "NOSDI002",
        "Open generic service cannot be registered by convention",
        "'{0}' matches a registration suffix but is an unbound generic type; register it explicitly instead",
        "NosCore.DependencyInjection",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.CompilationProvider.Select(static (compilation, _) => Collect(compilation));

        context.RegisterSourceOutput(candidates, static (spc, model) => Execute(model, spc));
    }

    private static RegistrationModel Collect(Compilation compilation)
    {
        var singletonMarker = compilation.GetTypeByMetadataName(SingletonMarker);
        if (singletonMarker is null)
        {
            return RegistrationModel.Empty;
        }

        // Both generators live in one analyzer assembly, so this runs for every project
        // that references it - including ones that merely reference NosCore.GameObject
        // and would resolve the marker from metadata. Only the assembly that declares
        // the marker owns these registrations.
        if (!SymbolEqualityComparer.Default.Equals(singletonMarker.ContainingAssembly, compilation.Assembly))
        {
            return RegistrationModel.Empty;
        }

        var nrunHandler = compilation.GetTypeByMetadataName(NrunEventHandler);
        var questHandler = compilation.GetTypeByMetadataName(QuestTypeHandler);

        var registrations = new List<Registration>();
        var diagnostics = new List<DiagnosticInfo>();

        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            // Static classes are abstract+sealed in metadata, so IsAbstract already
            // excludes them here exactly as it did in the runtime scan.
            if (type.TypeKind != TypeKind.Class || type.IsAbstract)
            {
                continue;
            }

            // Reflection's Type.IsPublic is true only for top-level public types; a
            // public nested type reports IsNestedPublic instead and was never picked up
            // by the runtime scan. Mirror that exactly so the generated list matches.
            var isTopLevelPublic = type.DeclaredAccessibility == Accessibility.Public &&
                                   type.ContainingType is null;

            var name = type.Name;
            var isUnboundGeneric = type.IsGenericType;
            var isSingleton = Implements(type, singletonMarker);
            var matchesSuffix = ConventionSuffixes.Any(s => name.EndsWith(s, System.StringComparison.Ordinal));
            var isHubClient = name.EndsWith(HubClientSuffix, System.StringComparison.Ordinal);

            // The four runtime scans ran independently over every type, so a class could
            // be picked up by more than one. Evaluate them independently here too rather
            // than short-circuiting, or the generated list silently loses registrations.
            if (isHubClient || matchesSuffix)
            {
                if (isUnboundGeneric)
                {
                    diagnostics.Add(DiagnosticInfo.For(UnregisterableOpenGeneric, type));
                    continue;
                }
            }

            // Hub clients are matched on name alone and registered as every interface
            // they implement, including framework ones - the runtime scan applied no
            // namespace filter and no visibility filter here, so neither do we.
            if (isHubClient)
            {
                foreach (var iface in type.AllInterfaces)
                {
                    registrations.Add(new Registration(Display(iface), Display(type), Lifetime.Singleton));
                }

                registrations.Add(new Registration(Display(type), Display(type), Lifetime.Singleton));
            }

            if (!isTopLevelPublic)
            {
                continue;
            }

            if (nrunHandler is not null && Implements(type, nrunHandler))
            {
                registrations.Add(new Registration(Display(nrunHandler), Display(type), Lifetime.Transient));
                registrations.Add(new Registration(Display(type), Display(type), Lifetime.Transient));
            }

            if (questHandler is not null && Implements(type, questHandler))
            {
                registrations.Add(new Registration(Display(questHandler), Display(type), Lifetime.Transient));
                registrations.Add(new Registration(Display(type), Display(type), Lifetime.Transient));
            }

            if (!matchesSuffix)
            {
                if (isSingleton)
                {
                    diagnostics.Add(DiagnosticInfo.For(DeadSingletonMarker, type));
                }

                continue;
            }

            var lifetime = isSingleton ? Lifetime.Singleton : Lifetime.Transient;

            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, singletonMarker) || IsSystemInterface(iface))
                {
                    continue;
                }

                registrations.Add(new Registration(Display(iface), Display(type), lifetime));
            }

            registrations.Add(new Registration(Display(type), Display(type), lifetime));
        }

        // Metadata order from Type.GetTypes() is not reproducible at compile time, so
        // order deterministically instead. Where an interface has several
        // implementations the last registration wins for GetRequiredService, which is
        // why DependencyInjectionParityTests asserts the generated set matches the
        // runtime scan as a set and flags multiply-implemented contracts.
        registrations.Sort(static (a, b) =>
        {
            var byService = string.CompareOrdinal(a.ServiceType, b.ServiceType);
            return byService != 0 ? byService : string.CompareOrdinal(a.ImplementationType, b.ImplementationType);
        });

        return new RegistrationModel(registrations.ToImmutableArray(), diagnostics.ToImmutableArray());
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol root)
    {
        foreach (var member in root.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol ns:
                    foreach (var nested in EnumerateTypes(ns))
                    {
                        yield return nested;
                    }

                    break;
                case INamedTypeSymbol type:
                    yield return type;
                    break;
            }
        }
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol iface)
        => type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iface));

    private static bool IsSystemInterface(INamedTypeSymbol iface)
    {
        var ns = iface.ContainingNamespace;
        if (ns is null || ns.IsGlobalNamespace)
        {
            return false;
        }

        return ns.ToDisplayString().StartsWith("System", System.StringComparison.Ordinal);
    }

    private static string Display(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static void Execute(RegistrationModel model, SourceProductionContext context)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor,
                diagnostic.Location,
                diagnostic.TypeName,
                string.Join(", ", ConventionSuffixes)));
        }

        if (model.Registrations.IsDefaultOrEmpty)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("//  __  _  __    __   ___ __  ___ ___");
        sb.AppendLine("// |  \\| |/__\\ /' _/ / _//__\\| _ \\ __|");
        sb.AppendLine("// | | ' | \\/ |`._`.| \\_| \\/ | v / _|");
        sb.AppendLine("// |_|\\__|\\__/ |___/ \\__/\\__/|_|_\\___|");
        sb.AppendLine("//");
        sb.AppendLine("// <auto-generated/> by NosCore.DiGenerator. Do not edit.");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace NosCore.GameObject.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class GeneratedServiceRegistrations");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void AddGeneratedGameObjectServices(this IServiceCollection services)");
        sb.AppendLine("        {");

        foreach (var registration in model.Registrations)
        {
            var method = registration.Lifetime == Lifetime.Singleton ? "AddSingleton" : "AddTransient";
            sb.Append("            services.").Append(method).Append('<')
                .Append(registration.ServiceType);

            if (registration.ServiceType != registration.ImplementationType)
            {
                sb.Append(", ").Append(registration.ImplementationType);
            }

            sb.AppendLine(">();");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // (serviceType, implementationType, lifetime) triples, exposed so");
        sb.AppendLine("        // DependencyInjectionParityTests can diff this list against the");
        sb.AppendLine("        // reflection scan it replaced.");
        sb.AppendLine("        public static readonly (string ServiceType, string ImplementationType, ServiceLifetime Lifetime)[] Descriptors =");
        sb.AppendLine("        {");

        foreach (var registration in model.Registrations)
        {
            var lifetime = registration.Lifetime == Lifetime.Singleton
                ? "ServiceLifetime.Singleton"
                : "ServiceLifetime.Transient";
            sb.Append("            (\"").Append(Strip(registration.ServiceType)).Append("\", \"")
                .Append(Strip(registration.ImplementationType)).Append("\", ")
                .Append(lifetime).AppendLine("),");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("GeneratedServiceRegistrations.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // Descriptors are compared against reflection-derived names in
    // DependencyInjectionParityTests, so drop every 'global::' - including the ones
    // inside type arguments of a constructed generic - rather than just the prefix.
    private static string Strip(string fullyQualified)
        => fullyQualified.Replace("global::", string.Empty);

    private enum Lifetime
    {
        Transient,
        Singleton
    }

    private readonly struct Registration
    {
        public Registration(string serviceType, string implementationType, Lifetime lifetime)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public string ServiceType { get; }
        public string ImplementationType { get; }
        public Lifetime Lifetime { get; }
    }

    // Deliberately stores strings and a Location rather than the INamedTypeSymbol:
    // symbols root the whole Compilation, and this value is cached by the incremental
    // pipeline.
    private readonly struct DiagnosticInfo
    {
        private DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, string typeName)
        {
            Descriptor = descriptor;
            Location = location;
            TypeName = typeName;
        }

        public DiagnosticDescriptor Descriptor { get; }
        public Location? Location { get; }
        public string TypeName { get; }

        public static DiagnosticInfo For(DiagnosticDescriptor descriptor, INamedTypeSymbol symbol)
            => new(descriptor, symbol.Locations.FirstOrDefault(), symbol.ToDisplayString());
    }

    private readonly struct RegistrationModel
    {
        public RegistrationModel(ImmutableArray<Registration> registrations, ImmutableArray<DiagnosticInfo> diagnostics)
        {
            Registrations = registrations;
            Diagnostics = diagnostics;
        }

        public ImmutableArray<Registration> Registrations { get; }
        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        public static RegistrationModel Empty => new(ImmutableArray<Registration>.Empty, ImmutableArray<DiagnosticInfo>.Empty);
    }
}
