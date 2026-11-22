//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// Only an NPC put up for a while carries one; it leaves its map once ExpiresAt passes.
public record struct ExpiryComponent(Instant ExpiresAt);
