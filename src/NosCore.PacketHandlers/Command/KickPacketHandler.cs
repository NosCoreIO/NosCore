//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class KickPacketHandler(IPubSubHub pubSubHub) : PacketHandler<KickPacket>,
        IWorldPacketHandler
    {
        public override async Task ExecuteAsync(KickPacket kickPacket, ClientSession session)
        {
            var accounts = await pubSubHub.GetSubscribersAsync();
            var receiver = accounts.FirstOrDefault(x => x.ConnectedCharacter?.Name == kickPacket.Name);

            if (receiver == null)
            {
                await session.SendPacketAsync(new InfoiPacket
                {
                    Message = Game18NConstString.UnknownCharacter
                });
                return;
            }

            await pubSubHub.UnsubscribeAsync(receiver.ConnectedCharacter!.Id);
        }
    }
}
