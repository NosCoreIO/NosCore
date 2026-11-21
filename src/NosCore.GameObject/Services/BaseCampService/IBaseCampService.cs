//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.BaseCampService;

public interface IBaseCampService
{
    bool HasCamp(FactionType faction);

    // Where the side's teleport scrolls lead, once its camp is built.
    (Guid MapInstanceId, short X, short Y)? FinishedCamp(FactionType faction);

    // Replaces any camp the side already has.
    Task BuildAsync(PlayerComponentBundle builder, short campVNum);

    // True when the monster was a side's camp.
    bool CampFell(MonsterComponentBundle monster);
}
