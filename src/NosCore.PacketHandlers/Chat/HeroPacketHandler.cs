//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ReputationRankingService;
using NosCore.Packets.ClientPackets.Chat;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Chat
{
    public class HeroPacketHandler(ISerializer packetSerializer, IPubSubHub pubSubHub,
            IReputationRankingService reputationRankingService)
        : PacketHandler<HeroPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(HeroPacket heroPacket, ClientSession session)
        {
            if (string.IsNullOrWhiteSpace(heroPacket.Message))
            {
                return;
            }

            var character = session.Character;
            if (!reputationRankingService.IsTopThree(character.CharacterId, character.Reput, session.Account.Authority))
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Red,
                    Message = Game18NConstString.OnlyTheTopThreePlayers
                });
                return;
            }

            // The client has no message that carries a player's free text, so msgi cannot replace msg here.
#pragma warning disable CS0618
            var heroMessage = new MsgPacket
            {
                Type = MessageType.Hero,
                Message = $"[{character.Name}]:{heroPacket.Message.Trim()}"
            };
#pragma warning restore CS0618

            await pubSubHub.SendMessageAsync(new PostedPacket
            {
                Packet = packetSerializer.Serialize(new[] { heroMessage }),
                SenderCharacter = new Character
                {
                    Name = character.Name,
                    Id = character.CharacterId
                },
                ReceiverType = ReceiverType.All
            });
        }
    }
}
