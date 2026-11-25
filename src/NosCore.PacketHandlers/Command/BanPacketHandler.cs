//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class BanPacketHandler(IPenaltyService penaltyService, IPubSubHub pubSubHub, IClock clock)
        : PacketHandler<BanPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BanPacket banPacket, ClientSession session)
        {
            var account = await penaltyService.FindAccountAsync(banPacket.Name, session.Character.ServerId);
            if (account == null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.UnknownCharacter });
                return;
            }

            var end = banPacket.Days == 0
                ? PenaltyService.Forever
                : clock.GetCurrentInstant().Plus(Duration.FromDays(banPacket.Days));
            await penaltyService.GiveAsync(account.AccountId, PenaltyType.Banned, end, banPacket.Reason ?? string.Empty,
                session.Character.Name);
            await pubSubHub.DisconnectAccountAsync(account.Name);
        }
    }
}
