//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class GuriPacketHandler(IGameEventDispatcher gameEvents) : PacketHandler<GuriPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(GuriPacket guriPacket, ClientSession session)
        {
            return gameEvents.RaiseAsync(new GuriPacketReceivedEvent(session, guriPacket));
        }
    }
}
