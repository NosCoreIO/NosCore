//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NodaTime;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Packets.ClientPackets.Families;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Family
{
    public class FhistCtsPacketHandler(IFamilyLogService familyLogService, IClock clock)
        : PacketHandler<FhistCtsPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FhistCtsPacket packet, ClientSession session)
        {
            var family = session.Character.Family;
            if (family == null || !family.CanGetHistory(session.Character.CharacterId))
            {
                return Task.CompletedTask;
            }

            return session.SendPacketAsync(familyLogService.GetHistory(family.FamilyId).GenerateGhis(clock.GetCurrentInstant()));
        }
    }
}
