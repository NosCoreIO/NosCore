//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Interfaces;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession;

public interface IPacketHandlingStrategy
{
    Task HandlePacketAsync(IPacket packet, ClientSession session, bool isFromNetwork);
}
