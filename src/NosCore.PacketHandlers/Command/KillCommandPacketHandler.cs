//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using Wolverine;

namespace NosCore.PacketHandlers.Command
{
    public class KillCommandPacketHandler(IMessageBus messageBus) : PacketHandler<KillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(KillPacket _, ClientSession session)
        {
            session.Character.Hp = 0;
            await messageBus.PublishAsync(new EntityDiedEvent(session.Character, session.Character)).ConfigureAwait(false);
        }
    }
}
