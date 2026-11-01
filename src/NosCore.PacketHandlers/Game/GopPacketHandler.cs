//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.UI;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Game
{
    public class GopPacketHandler(IPubSubHub pubSubHub, ILogger<GopPacketHandler> logger)
        : PacketHandler<GopPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GopPacket packet, ClientSession session)
        {
            var character = session.Character;
            if (!character.TryApplyOption(packet.Option, packet.IsActive, out var message))
            {
                return;
            }

            // Friend requests and whispers are checked against the master's subscriber list,
            // so it has to see the new state.
            if (packet.Option is GopPacketType.FriendRequestBlocked or GopPacketType.WhisperBlocked)
            {
                var published = await pubSubHub.SubscribeAsync(new Subscriber
                {
                    Id = session.SessionId,
                    Name = session.Account.Name,
                    Language = session.Account.Language,
                    ConnectedCharacter = new Character
                    {
                        Id = character.CharacterId,
                        Name = character.Name,
                        FriendRequestBlocked = character.FriendRequestBlocked,
                        WhisperBlocked = character.WhisperBlocked
                    }
                });
                if (!published)
                {
                    logger.LogWarning("The master server rejected the {Option} option of {CharacterId}; other channels keep the previous value until the next login",
                        packet.Option, character.CharacterId);
                }
            }

            if (message != null)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = message.Value
                });
            }

            await session.SendPacketAsync(character.GenerateStat());
        }
    }
}
