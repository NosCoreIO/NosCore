using System.Threading.Tasks;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Packets.Enumerations;
using NosCore.GameObject.Services.BroadcastService;

using MailData = NosCore.GameObject.InterChannelCommunication.Messages.MailData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class MailDataMessageChannelCommunicationMessageHandler(IGameLanguageLocalizer gameLanguageLocalizer, ISessionRegistry sessionRegistry, IEntityPacketSystem entityPacketSystem) : ChannelCommunicationMessageHandler<MailData>
    {
        public override async Task Handle(MailData data)
        {
            var player = sessionRegistry.GetPlayer(p => p.Name == data.ReceiverName);

            if (player == null)
            {
                return;
            }

            var playerValue = player.Value;
            var sender = sessionRegistry.GetSenderByCharacterId(playerValue.CharacterId);

            if (data.ItemInstance != null)
            {
                await (sender?.SendPacketAsync(entityPacketSystem.GenerateSay(playerValue,
                    string.Format(gameLanguageLocalizer[LanguageKey.ITEM_GIFTED, playerValue.AccountLanguage],
                        data.ItemInstance.Amount), SayColorType.Green)) ?? Task.CompletedTask).ConfigureAwait(false);
            }

            var postType = data.ItemInstance != null ? (byte)0 : (byte)1;
            var post = data.GeneratePost(postType);
            if (post != null)
            {
                await (sender?.SendPacketAsync(post) ?? Task.CompletedTask).ConfigureAwait(false);
            }
        }
    }
}
