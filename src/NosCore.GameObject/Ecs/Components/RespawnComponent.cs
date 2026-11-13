//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;

namespace NosCore.GameObject.Ecs.Components;

// RespawnAt is null while the entity is up; an entity that does not respawn stays down.
public record struct RespawnComponent(Instant? RespawnAt, bool Respawns);
