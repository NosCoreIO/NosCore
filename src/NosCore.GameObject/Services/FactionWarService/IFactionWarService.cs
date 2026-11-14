//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Families;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FactionWarService;

public interface IFactionWarService
{
    // Hundredths of a percent, 0 to 10000.
    int Progress(FactionType faction);

    FcPacket GenerateFc(PlayerComponentBundle player);

    Task RecordKillAsync(PlayerComponentBundle killer, PlayerComponentBundle victim);
}
