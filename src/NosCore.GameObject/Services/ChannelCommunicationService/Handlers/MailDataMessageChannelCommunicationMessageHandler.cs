//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Packets.Enumerations;
using System.Threading.Tasks;
using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class MailDataMessageChannelCommunicationMessageHandler(IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<MailData>
    {
        public override async Task Handle(MailData data)
        {
            var session = sessionRegistry.GetSession(s => s.Character.Name == data.ReceiverName);

            if (session == null)
            {
                return;
            }

            var character = session.Character;
            if (data.ItemInstance != null)
            {
                await session.SendPacketAsync(character.GenerateSay(
                    string.Format(gameLanguageLocalizer[LanguageKey.ITEM_GIFTED, character.AccountLanguage],
                        data.ItemInstance.Amount), SayColorType.Green));
            }

            await session.GenerateMailAsync(new[] { data });
        }
    }
}
