//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Command
{
    public class UnbanPacketHandler(IPenaltyService penaltyService, IGameLanguageLocalizer gameLanguageLocalizer) : PacketHandler<UnbanPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UnbanPacket packet, ClientSession session)
        {
            var account = await penaltyService.FindAccountAsync(packet.Name, session.Character.ServerId);
            if (account == null)
            {
                await session.SendPacketAsync(new InfoiPacket { Message = Game18NConstString.UnknownCharacter });
                return;
            }

            if (!penaltyService.MayPunish(session.Account, account))
            {
                await session.SendPacketAsync(session.Character.GenerateSay(
                    gameLanguageLocalizer[LanguageKey.CANNOT_PUNISH_ACCOUNT, session.Account.Language], SayColorType.Red));
                return;
            }

            await penaltyService.LiftAsync(account.AccountId, PenaltyType.Banned);
        }
    }
}
