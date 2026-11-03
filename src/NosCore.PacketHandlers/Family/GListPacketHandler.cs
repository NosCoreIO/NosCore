//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.Algorithm.FamilyExperienceService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using NosCore.Packets.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class GListPacketHandler(IFamilyService familyService, IPubSubHub pubSubHub,
            IFamilyExperienceService familyExperienceService)
        : PacketHandler<GListPacket>, IWorldPacketHandler
    {
        private const byte MemberList = 2;

        public override async Task ExecuteAsync(GListPacket packet, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || packet.Type != MemberList)
            {
                return;
            }

            var characters = await familyService.GetMemberCharactersAsync(family);
            var connected = (await pubSubHub.GetSubscribersAsync())
                .Where(s => s.ConnectedCharacter != null)
                .Select(s => s.ConnectedCharacter!.Id)
                .ToHashSet();
            await session.SendPacketsAsync(new IPacket?[]
            {
                session.Character.GenerateGInfo(familyExperienceService),
                family.GenerateGmbr(characters, connected),
                family.GenerateGmsg(),
                family.GenerateGexp()
            }.OfType<IPacket>());
        }
    }
}
