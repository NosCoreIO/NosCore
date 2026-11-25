//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
    public class UnmutePacketHandler(IPenaltyService penaltyService) : PacketHandler<UnmutePacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UnmutePacket packet, ClientSession session)
        {
            var account = await penaltyService.FindAccountAsync(packet.Name, session.Character.ServerId);
            if (account == null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.UnknownCharacter });
                return;
            }

            await penaltyService.LiftAsync(account.AccountId, PenaltyType.Muted);
        }
    }
}
