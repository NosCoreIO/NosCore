//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NosCore.DiGenerator;

// Emits the concrete world/login packet handler lists the server bootstraps used to
// discover by scanning NosCore.PacketHandlers with reflection, and turns the two
// failure modes that scan swallowed into build errors.
[Generator]
public class PacketHandlerGenerator : IIncrementalGenerator
{
    private const string PacketHandlerBase = "NosCore.GameObject.Infastructure.PacketHandler`1";
    private const string WorldMarker = "NosCore.GameObject.Infastructure.IWorldPacketHandler";
    private const string LoginMarker = "NosCore.GameObject.Infastructure.ILoginPacketHandler";
    private const string PacketHeaderAttribute = "NosCore.Packets.Attributes.PacketHeaderAttribute";

    private static readonly DiagnosticDescriptor DuplicateHandler = new(
        "NOSDI003",
        "Two packet handlers claim the same packet",
        "'{0}' and '{1}' both handle {2} in the same server scope; only one would ever be dispatched",
        "NosCore.DependencyInjection",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingPacketHeader = new(
        "NOSDI004",
        "Handled packet has no PacketHeader attribute",
        "'{0}' handles {1}, which carries no [PacketHeader]; the client can never address it",
        "NosCore.DependencyInjection",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnscopedHandler = new(
        "NOSDI005",
        "Packet handler declares no server scope",
        "'{0}' derives from PacketHandler<T> but implements neither IWorldPacketHandler nor ILoginPacketHandler, so no bootstrap registers it",
        "NosCore.DependencyInjection",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Opt-in per project. Every assembly referencing NosCore.GameObject can see
        // PacketHandler<T>, but only NosCore.PacketHandlers should own the generated
        // bootstrap lists, so it sets NosCoreGeneratePacketHandlerLists in its csproj.
        var enabled = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            provider.GlobalOptions.TryGetValue(
                "build_property.NosCoreGeneratePacketHandlerLists", out var value) &&
            value.Equals("true", StringComparison.OrdinalIgnoreCase));

        var handlers = context.CompilationProvider.Combine(enabled)
            .Select(static (pair, _) => pair.Right ? Collect(pair.Left) : HandlerModel.Empty);

        context.RegisterSourceOutput(handlers, static (spc, model) => Execute(model, spc));
    }

    private static HandlerModel Collect(Compilation compilation)
    {
        var baseType = compilation.GetTypeByMetadataName(PacketHandlerBase);
        var worldMarker = compilation.GetTypeByMetadataName(WorldMarker);
        var loginMarker = compilation.GetTypeByMetadataName(LoginMarker);
        var headerAttribute = compilation.GetTypeByMetadataName(PacketHeaderAttribute);

        if (baseType is null || worldMarker is null || loginMarker is null)
        {
            return HandlerModel.Empty;
        }

        var handlers = new List<Handler>();
        var diagnostics = new List<DiagnosticInfo>();

        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsGenericType)
            {
                continue;
            }

            var packetType = FindHandledPacket(type, baseType);
            if (packetType is null)
            {
                continue;
            }

            var isWorld = Implements(type, worldMarker);
            var isLogin = Implements(type, loginMarker);

            if (!isWorld && !isLogin)
            {
                diagnostics.Add(DiagnosticInfo.For(UnscopedHandler, type, type.ToDisplayString(), string.Empty));
                continue;
            }

            if (headerAttribute is not null && !HasHeader(packetType, headerAttribute))
            {
                diagnostics.Add(DiagnosticInfo.For(MissingPacketHeader, type,
                    type.ToDisplayString(), packetType.ToDisplayString()));
            }

            handlers.Add(new Handler(
                Display(type),
                Display(packetType),
                packetType.ToDisplayString(),
                isWorld,
                isLogin,
                type.Locations.FirstOrDefault(),
                type.ToDisplayString()));
        }

        foreach (var scope in new[] { true, false })
        {
            var inScope = handlers.Where(h => scope ? h.IsWorld : h.IsLogin);

            foreach (var clash in inScope.GroupBy(h => h.PacketDisplay).Where(g => g.Count() > 1))
            {
                var ordered = clash.OrderBy(h => h.HandlerDisplay, StringComparer.Ordinal).ToList();
                diagnostics.Add(DiagnosticInfo.At(DuplicateHandler, ordered[1].Location,
                    ordered[0].HandlerDisplay, ordered[1].HandlerDisplay, clash.Key));
            }
        }

        handlers.Sort(static (a, b) => string.CompareOrdinal(a.HandlerType, b.HandlerType));

        return new HandlerModel(handlers.ToImmutableArray(), diagnostics.ToImmutableArray());
    }

    private static INamedTypeSymbol? FindHandledPacket(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
            {
                return current.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
            }
        }

        return null;
    }

    // PacketHeaderAttribute is inherited (CommandPacketHeaderAttribute derives from it),
    // so walk the packet's base chain the way GetCustomAttribute(inherit: true) did.
    private static bool HasHeader(INamedTypeSymbol packetType, INamedTypeSymbol headerAttribute)
    {
        for (var current = packetType; current is not null; current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                for (var attrType = attribute.AttributeClass; attrType is not null; attrType = attrType.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(attrType, headerAttribute))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
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

    private static string Display(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static void Execute(HandlerModel model, SourceProductionContext context)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor, diagnostic.Location, diagnostic.Args));
        }

        if (model.Handlers.IsDefaultOrEmpty)
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
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace NosCore.PacketHandlers.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class GeneratedPacketHandlers");
        sb.AppendLine("    {");

        AppendTypeArray(sb, "WorldHandlerTypes", model.Handlers.Where(h => h.IsWorld));
        sb.AppendLine();
        AppendTypeArray(sb, "LoginHandlerTypes", model.Handlers.Where(h => h.IsLogin));

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("GeneratedPacketHandlers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void AppendTypeArray(StringBuilder sb, string name, IEnumerable<Handler> handlers)
    {
        sb.Append("        public static readonly Type[] ").Append(name).AppendLine(" =");
        sb.AppendLine("        {");

        foreach (var handler in handlers)
        {
            sb.Append("            typeof(").Append(handler.HandlerType).AppendLine("),");
        }

        sb.AppendLine("        };");
    }

    private readonly struct Handler
    {
        public Handler(string handlerType, string packetType, string packetDisplay, bool isWorld, bool isLogin,
            Location? location, string handlerDisplay)
        {
            HandlerType = handlerType;
            PacketType = packetType;
            PacketDisplay = packetDisplay;
            IsWorld = isWorld;
            IsLogin = isLogin;
            Location = location;
            HandlerDisplay = handlerDisplay;
        }

        public string HandlerType { get; }
        public string PacketType { get; }
        public string PacketDisplay { get; }
        public bool IsWorld { get; }
        public bool IsLogin { get; }
        public Location? Location { get; }
        public string HandlerDisplay { get; }
    }

    private readonly struct DiagnosticInfo
    {
        private DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, object?[] args)
        {
            Descriptor = descriptor;
            Location = location;
            Args = args;
        }

        public DiagnosticDescriptor Descriptor { get; }
        public Location? Location { get; }
        public object?[] Args { get; }

        public static DiagnosticInfo For(DiagnosticDescriptor descriptor, INamedTypeSymbol symbol, params object?[] args)
            => new(descriptor, symbol.Locations.FirstOrDefault(), args);

        public static DiagnosticInfo At(DiagnosticDescriptor descriptor, Location? location, params object?[] args)
            => new(descriptor, location, args);
    }

    private readonly struct HandlerModel
    {
        public HandlerModel(ImmutableArray<Handler> handlers, ImmutableArray<DiagnosticInfo> diagnostics)
        {
            Handlers = handlers;
            Diagnostics = diagnostics;
        }

        public ImmutableArray<Handler> Handlers { get; }
        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        public static HandlerModel Empty => new(ImmutableArray<Handler>.Empty, ImmutableArray<DiagnosticInfo>.Empty);
    }
}
