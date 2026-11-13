//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.PenaltyService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Chat
{
    public class ClientSayPacketHandler(IPenaltyService penaltyService) : PacketHandler<ClientSayPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(ClientSayPacket clientSayPacket, ClientSession session)
        {
            var character = session.Character;
            var muteLeft = await penaltyService.GetMuteLeftAsync(session.Account.AccountId);
            if (muteLeft is { } left)
            {
                await character.MapInstance.SendPacketAsync(
                    character.GenerateSayi(SayColorType.BubbleOnly, Game18NConstString.IAmUnderPenalty));
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Red,
                    Game18NConstString.RemainingPunishment, 4, (long)Math.Ceiling(left.TotalMinutes)));
                return;
            }

            await character.MapInstance.SendPacketAsync(character.GenerateSay(new SayPacket
            {
                Message = clientSayPacket.Message,
                Type = SayColorType.Default
            }), new EveryoneBut(session.ConnectedChannel.Id));
        }
    }
}
