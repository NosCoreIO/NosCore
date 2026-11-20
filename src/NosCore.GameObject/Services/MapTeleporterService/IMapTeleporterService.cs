//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.MapTeleporterService;

public interface IMapTeleporterService
{
    // Starts the bar when the NPC carries players across its own map.
    Task<bool> StartAsync(PlayerComponentBundle character, NpcComponentBundle npc);

    // Moves the player and their mates to the NPC's destination once its bar has run.
    Task<bool> CompleteAsync(PlayerComponentBundle character, int mapNpcId, short x, short y);
}
