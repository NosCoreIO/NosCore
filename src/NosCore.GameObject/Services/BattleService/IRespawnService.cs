//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Services.BattleService;

public interface IRespawnService
{
    // The MapType.RespawnMapTypeId for the given MapId, or null if the map isn't tied
    // to a respawn group. OpenNos keys per-character saved respawns by this value so a
    // save-point NPC in Act 5 doesn't overwrite the Act 1 save.
    long? ResolveRespawnMapTypeId(short mapId);

    // The (mapId, x, y) the character should respawn at for the given MapType group.
    // Falls back to the RespawnMapType catalog default (Nosville for Act 1).
    (short MapId, short X, short Y) GetRespawnLocation(ICharacterEntity character, long? respawnMapTypeId);

    // Upserts the character's Respawn row for the MapType group to (mapId, x, y).
    // No-op if the map has no RespawnMapTypeId.
    void SetRespawnPoint(ICharacterEntity character, short mapId, short x, short y);

    // The point of return saved on a map of the same MapType group, or null if none was saved.
    (short MapId, short X, short Y)? GetReturnPoint(ICharacterEntity character, short mapId);

    // Upserts the character's point of return for the map's MapType group (Act 1 when it has none).
    void SetReturnPoint(ICharacterEntity character, short mapId, short x, short y);
}
