using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Serilog;

using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class PostedPacketMessageChannelCommunicationMessageHandler(ILogger logger, IDeserializer deserializer,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IPubSubHub pubSubHub) : ChannelCommunicationMessageHandler<PostedPacket>
    {
        public override async Task Handle(PostedPacket postedPacket)
        {
            var message = deserializer.Deserialize(postedPacket.Packet!);
            switch (postedPacket.ReceiverType)
            {
                case ReceiverType.All:
                    await Broadcaster.Instance.SendPacketAsync(message).ConfigureAwait(false);
                    await pubSubHub.DeleteMessageAsync(postedPacket.Id);
                    break;
                case ReceiverType.OnlySomeone:
                    ICharacterEntity? receiverSession;

                    if (postedPacket.ReceiverCharacter!.Name != null)
                    {
                        receiverSession = Broadcaster.Instance.GetCharacter(s =>
                            s.Name == postedPacket.ReceiverCharacter.Name);
                    }
                    else
                    {
                        receiverSession = Broadcaster.Instance.GetCharacter(s =>
                            s.VisualId == postedPacket.ReceiverCharacter.Id);
                    }

                    if (receiverSession == null)
                    {
                        return;
                    }

                    await receiverSession.SendPacketAsync(message).ConfigureAwait(false);
                    await pubSubHub.DeleteMessageAsync(postedPacket.Id);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    await pubSubHub.DeleteMessageAsync(postedPacket.Id);
                    break;
            }

        }
    }
}
