//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class MutePacketHandler(IPenaltyService penaltyService, IClock clock)
        : PacketHandler<MutePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(MutePacket mutePacket, ClientSession session)
        {
            var account = await penaltyService.FindAccountAsync(mutePacket.Name, session.Character.ServerId);
            if (account == null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.UnknownCharacter });
                return;
            }

            await penaltyService.GiveAsync(account.AccountId, PenaltyType.Muted,
                clock.GetCurrentInstant().Plus(Duration.FromMinutes(mutePacket.Minutes)), mutePacket.Reason ?? string.Empty,
                session.Character.Name);
            await session.SendPacketAsync(new Msgi2Packet
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HasBeenPunished,
                ArgumentType = 3,
                Game18NArguments = { mutePacket.Name ?? string.Empty, mutePacket.Minutes },
            });
        }
    }
}
