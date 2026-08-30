; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
NOSDI001 | NosCore.DependencyInjection | Warning | ServiceRegistrationGenerator, ISingletonService marker has no effect
NOSDI002 | NosCore.DependencyInjection | Warning | ServiceRegistrationGenerator, open generic service cannot be registered by convention
NOSDI003 | NosCore.DependencyInjection | Error | PacketHandlerGenerator, two packet handlers claim the same packet
NOSDI004 | NosCore.DependencyInjection | Warning | PacketHandlerGenerator, handled packet has no PacketHeader attribute
NOSDI005 | NosCore.DependencyInjection | Warning | PacketHandlerGenerator, packet handler declares no server scope
