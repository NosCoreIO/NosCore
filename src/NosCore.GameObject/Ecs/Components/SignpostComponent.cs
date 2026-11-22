//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// A Miniland signpost leads to its owner's Miniland until it comes down; an owner of 0 is any other NPC.
public record struct SignpostComponent(long SignpostOwnerId, string? SignpostOwnerName, Instant SignpostExpiresAt);
