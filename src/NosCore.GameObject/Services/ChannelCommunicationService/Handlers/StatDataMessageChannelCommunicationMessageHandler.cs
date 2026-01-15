using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using NosCore.GameObject.Services.BroadcastService;

using StatData = NosCore.GameObject.InterChannelCommunication.Messages.StatData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class StatDataMessageChannelCommunicationMessageHandler(ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IPubSubHub pubSubHub, IOptions<WorldConfiguration> worldConfiguration, ISessionRegistry sessionRegistry) : ChannelCommunicationMessageHandler<StatData>
    {
        public override async Task Handle(StatData data)
        {
            var session = sessionRegistry.GetCharacter(s => s.Name == data.Character?.Name);

            if (session == null)
            {
                return;
            }

            switch (data.ActionType)
            {
                case UpdateStatActionType.UpdateLevel:
                    session.SetLevel((byte)data.Data);
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    await session.SetJobLevelAsync((byte)data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    await session.SetHeroLevelAsync((byte)data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateReputation:
                    await session.SetReputationAsync(data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateGold:
                    if (session.Gold + data.Data > worldConfiguration.Value.MaxGoldAmount)
                    {
                        await pubSubHub.DeleteMessageAsync(data.Id);
                        return;
                    }

                    await session.SetGoldAsync(data.Data).ConfigureAwait(false);
                    break;
                case UpdateStatActionType.UpdateClass:
                    await session.ChangeClassAsync((CharacterClassType)data.Data).ConfigureAwait(false);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }

            await pubSubHub.DeleteMessageAsync(data.Id);

        }
    }
}
