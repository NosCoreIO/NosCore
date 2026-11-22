//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;

namespace NosCore.GameObject.Services.MinilandService;

public interface IMinilandSignpostService
{
    bool AllowsSignposts(MapInstance map);

    // Null while the owner has no signpost up.
    Duration? TimeLeft(long ownerId);

    Task PlaceAsync(PlayerComponentBundle owner, NpcMonsterDto signpost);

    // False when the NPC is no signpost, so it is talked to as usual.
    Task<bool> VisitAsync(ClientSession visitor, NpcComponentBundle npc);

    Task TakeDownAsync(long ownerId);
}
