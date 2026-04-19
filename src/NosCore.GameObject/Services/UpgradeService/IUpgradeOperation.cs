//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.UpgradeService;

public interface IUpgradeOperation
{
    UpgradePacketType Kind { get; }

    Task<IReadOnlyList<IPacket>> ExecuteAsync(ClientSession session, UpgradePacket packet);
}
