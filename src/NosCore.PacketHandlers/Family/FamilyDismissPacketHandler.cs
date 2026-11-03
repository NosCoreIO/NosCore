//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Commands;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FamilyDismissPacketHandler(IFamilyService familyService, IPubSubHub pubSubHub)
        : PacketHandler<FamilyDismissPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(FamilyDismissPacket packet, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || string.IsNullOrWhiteSpace(packet.CharacterName))
            {
                return;
            }

            var authority = family.AuthorityOf(session.Character.CharacterId);
            if (authority != FamilyAuthority.Head && authority != FamilyAuthority.Assistant)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.NotAuthorizedToAcceptFamily });
                return;
            }

            var target = await familyService.FindCharacterAsync(packet.CharacterName);
            var membership = target == null ? null : family.Members.FirstOrDefault(s => s.CharacterId == target.CharacterId);
            if (target == null || membership == null)
            {
                await session.SendPacketAsync(new Infoi2Packet
                {
                    Message = Game18NConstString.CanNotFind,
                    ArgumentType = 1,
                    Game18NArguments = { packet.CharacterName }
                });
                return;
            }

            // A lower value is a higher rank: nobody dismisses the head, their own rank or above.
            if (membership.Authority <= authority)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.CanNotDismissedDeputy });
                return;
            }

            await familyService.RemoveMemberAsync(target.CharacterId);
            await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.Dismissed2 });
            await pubSubHub.SendMessageAsync(new FamilyChangedData
            {
                FamilyId = family.FamilyId,
                CharacterIds = { target.CharacterId }
            });
        }
    }
}
