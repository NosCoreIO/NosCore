using System.Threading.Tasks;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.ComponentEntities.Interfaces;
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
                    await sessionRegistry.BroadcastPacketAsync(message);
                    break;
                case ReceiverType.OnlySomeone:
                    ICharacterEntity? receiverSession;

                    if (postedPacket.ReceiverCharacter!.Name != null)
                    {
                        receiverSession = sessionRegistry.GetCharacter(s =>
                            s.Name == postedPacket.ReceiverCharacter.Name);
                    }
                    else
                    {
                        receiverSession = sessionRegistry.GetCharacter(s =>
                            s.VisualId == postedPacket.ReceiverCharacter.Id);
                    }

                    if (receiverSession == null)
                    {
                        return;
                    }

                    await receiverSession.SendPacketAsync(message);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }
        }
    }
}
