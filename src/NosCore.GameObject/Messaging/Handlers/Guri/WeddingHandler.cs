//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // Argument 1 accepts the proposal from VisualId, argument 0 declines it.
    [UsedImplicitly]
    public sealed class WeddingHandler(IFriendHub friendHub, IChannelHub channelHub, IPubSubHub pubSubHub,
        ISessionRegistry sessionRegistry)
    {
        private const int Accept = 1;

        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.Wedding || packet.VisualId is not { } proposerId)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (sessionRegistry.GetSessionByCharacterId(proposerId)?.Character is not { } proposer
                || !proposer.MarriageProposalCharacterIds.TryRemove(character.CharacterId, out _))
            {
                return;
            }

            if (packet.Argument != Accept)
            {
                await BroadcastAsync(new Msgi2Packet
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.ProposalDeclined,
                    ArgumentType = 1,
                    Game18NArguments = { character.Name, proposer.Name }
                });
                return;
            }

            if (!await friendHub.MarryAsync(proposer.CharacterId, character.CharacterId))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CannotAcceptProposal
                });
                return;
            }

            await BroadcastAsync(new Msgi2Packet
            {
                Type = MessageType.Default,
                Message = Game18NConstString.PlayersMarried,
                ArgumentType = 1,
                Game18NArguments = { proposer.Name, character.Name }
            });
            await session.SendPacketAsync(await character.GenerateFinitAsync(friendHub, channelHub, pubSubHub));
            await proposer.SendPacketAsync(await proposer.GenerateFinitAsync(friendHub, channelHub, pubSubHub));
        }

        private Task BroadcastAsync(Msgi2Packet packet) =>
            Task.WhenAll(sessionRegistry.GetSessions(s => s.HasSelectedCharacter).Select(s => s.SendPacketAsync(packet)));
    }
}
