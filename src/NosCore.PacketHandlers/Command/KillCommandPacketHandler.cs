//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Command
{
    public class KillCommandPacketHandler(IGameEventDispatcher gameEvents) : PacketHandler<KillPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(KillPacket _, ClientSession session)
        {
            if (session.Character.Hp <= 0)
            {
                return;
            }

            session.Character.Hp = 0;
            await gameEvents.RaiseAsync(new EntityDiedEvent(session.Character, session.Character));
        }
    }
}
