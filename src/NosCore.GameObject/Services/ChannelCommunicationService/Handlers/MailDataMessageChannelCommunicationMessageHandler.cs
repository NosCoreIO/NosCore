using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.Packets.Enumerations;

using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class MailDataMessageChannelCommunicationMessageHandler(IPubSubHub pubSubHub, IGameLanguageLocalizer gameLanguageLocalizer) : ChannelCommunicationMessageHandler<MailData>
    {
        public override async Task Handle(MailData data)
        {
            var session = Broadcaster.Instance.GetCharacter(s => s.Name == data.ReceiverName);

            if (session == null)
            {
                return;
            }

            if (data.ItemInstance != null)
            {
                await session.SendPacketAsync(session.GenerateSay(
                    string.Format(gameLanguageLocalizer[LanguageKey.ITEM_GIFTED, session.AccountLanguage],
                        data.ItemInstance.Amount), SayColorType.Green)).ConfigureAwait(false);
            }

            await session.GenerateMailAsync(new[] { data }).ConfigureAwait(false);
            await pubSubHub.DeleteMessageAsync(data.Id);

        }
    }
}
