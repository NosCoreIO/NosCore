//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Interfaces;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using PostedPacket = NosCore.GameObject.InterChannelCommunication.Messages.PostedPacket;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class PostedPacketMessageChannelCommunicationMessageHandler(ILogger<PostedPacketMessageChannelCommunicationMessageHandler> logger, IDeserializer deserializer,
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
                    ClientSession? receiverSession;

                    if (postedPacket.ReceiverCharacter!.Name != null)
                    {
                        receiverSession = sessionRegistry.GetSession(s =>
                            s.Character.Name == postedPacket.ReceiverCharacter.Name);
                    }
                    else
                    {
                        receiverSession = sessionRegistry.GetSession(s =>
                            s.Character.VisualId == postedPacket.ReceiverCharacter.Id);
                    }

                    if (receiverSession == null)
                    {
                        return;
                    }

                    await receiverSession.SendPacketAsync(message);
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }
        }
    }
}
