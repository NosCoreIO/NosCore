//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BroadcastService
{
    public interface IPacketBroadcaster
    {
        Task SendToAsync(IPacketTarget target, IPacket packet);
        Task SendToAsync(IPacketTarget target, IEnumerable<IPacket> packets);
    }
}
