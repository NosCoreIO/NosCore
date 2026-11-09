//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.CollectService;

public interface ICollectService
{
    // Starts the collect bar when the NPC is a collectable map object still standing.
    Task<bool> StartAsync(PlayerComponentBundle character, NpcComponentBundle npc);

    // Gives the object's item once its bar has run, then hides it until it respawns.
    Task<bool> CompleteAsync(PlayerComponentBundle character, int mapNpcId);
}
