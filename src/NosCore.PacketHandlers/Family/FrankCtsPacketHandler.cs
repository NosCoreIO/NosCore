//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FrankCtsPacketHandler(IFamilyRankingService familyRankingService)
        : PacketHandler<FrankCtsPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FrankCtsPacket packet, ClientSession session) =>
            session.SendPacketAsync(familyRankingService.GetRanking(packet.Type).GenerateFrank(packet.Type));
    }
}
