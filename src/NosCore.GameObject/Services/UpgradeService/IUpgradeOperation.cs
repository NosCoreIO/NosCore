//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Shops;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Player;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;

namespace NosCore.GameObject.Services.UpgradeService;

public interface IUpgradeOperation
{
    UpgradePacketType Kind { get; }

    // The dialog that has to be open for the window to be used.
    NpcDialogWindow Window { get; }

    // The shop tab that offers it, or null for a window opened by an item.
    NpcService? Service { get; }

    Task<IReadOnlyList<IPacket>> ExecuteAsync(ClientSession session, UpgradePacket packet);
}
