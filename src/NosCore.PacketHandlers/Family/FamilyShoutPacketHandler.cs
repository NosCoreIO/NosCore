//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Commands;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FamilyShoutPacketHandler(IPubSubHub pubSubHub) : PacketHandler<FamilyShoutPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FamilyShoutPacket familyShoutPacket, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || string.IsNullOrWhiteSpace(familyShoutPacket.Message))
            {
                return;
            }

            var canShout = family.AuthorityOf(session.Character.CharacterId) switch
            {
                FamilyAuthority.Head or FamilyAuthority.Assistant => true,
                FamilyAuthority.Manager => family.ManagerCanShout,
                _ => false
            };
            if (!canShout)
            {
                return;
            }

            await pubSubHub.SendMessageAsync(new FamilyShoutData
            {
                FamilyId = family.FamilyId,
                Message = familyShoutPacket.Message.Trim()
            });
        }
    }
}
