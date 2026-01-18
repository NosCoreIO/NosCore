using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Serilog;
using NosCore.GameObject.Services.BroadcastService;

using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class PostedPacketMessageChannelCommunicationMessageHandler(ILogger logger, IDeserializer deserializer,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<PostedPacket>
    {
        public override async Task Handle(PostedPacket postedPacket)
        {
            var message = deserializer.Deserialize(postedPacket.Packet!);
            switch (postedPacket.ReceiverType)
            {
                case ReceiverType.All:
                    await sessionRegistry.BroadcastPacketAsync(message).ConfigureAwait(false);
                    break;
                case ReceiverType.OnlySomeone:
                    PlayerContext? receiverPlayer;

                    if (postedPacket.ReceiverCharacter!.Name != null)
                    {
                        receiverPlayer = sessionRegistry.GetPlayer(p => p.Name == postedPacket.ReceiverCharacter.Name);
                    }
                    else
                    {
                        receiverPlayer = sessionRegistry.GetPlayer(p => p.VisualId == postedPacket.ReceiverCharacter.Id);
                    }

                    if (receiverPlayer == null)
                    {
                        return;
                    }

                    var sender = sessionRegistry.GetSenderByCharacterId(receiverPlayer.Value.CharacterId);
                    await (sender?.SendPacketAsync(message) ?? Task.CompletedTask).ConfigureAwait(false);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }
        }
    }
}
