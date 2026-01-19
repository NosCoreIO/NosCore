using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.BroadcastService;

using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class MailDataMessageChannelCommunicationMessageHandler(IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<MailData>
    {
        public override async Task Handle(MailData data)
        {
            var session = sessionRegistry.GetCharacter(s => s.Name == data.ReceiverName);

            if (session == null)
            {
                return;
            }

            if (data.ItemInstance != null)
            {
                await session.SendPacketAsync(session.GenerateSay(
                    string.Format(gameLanguageLocalizer[LanguageKey.ITEM_GIFTED, session.AccountLanguage],
                        data.ItemInstance.Amount), SayColorType.Green));
            }

            await session.GenerateMailAsync(new[] { data });
        }
    }
}
