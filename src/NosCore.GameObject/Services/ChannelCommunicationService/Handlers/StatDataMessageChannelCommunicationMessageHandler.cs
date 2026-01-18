using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Systems;
using NosCore.Shared.I18N;
using Serilog;
using NosCore.GameObject.Services.BroadcastService;

using StatData = NosCore.GameObject.InterChannelCommunication.Messages.StatData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class StatDataMessageChannelCommunicationMessageHandler(ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration, ISessionRegistry sessionRegistry, IStatsSystem statsSystem) : ChannelCommunicationMessageHandler<StatData>
    {
        public override Task Handle(StatData data)
        {
            var player = sessionRegistry.GetPlayer(p => p.Name == data.Character?.Name);

            if (player == null)
            {
                return Task.CompletedTask;
            }

            var playerValue = player.Value;

            switch (data.ActionType)
            {
                case UpdateStatActionType.UpdateLevel:
                    statsSystem.SetLevel(playerValue, (byte)data.Data);
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    playerValue.SetJobLevel((byte)data.Data);
                    playerValue.SetJobLevelXp(0);
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    playerValue.Entity.SetHeroLevel(playerValue.World, (byte)data.Data);
                    playerValue.SetHeroXp(0);
                    break;
                case UpdateStatActionType.UpdateReputation:
                    playerValue.SetReput(data.Data);
                    break;
                case UpdateStatActionType.UpdateGold:
                    var currentGold = playerValue.Gold;
                    if (currentGold + data.Data > worldConfiguration.Value.MaxGoldAmount)
                    {
                        return Task.CompletedTask;
                    }

                    playerValue.SetGold(data.Data);
                    break;
                case UpdateStatActionType.UpdateClass:
                    // TODO: ChangeClassAsync needs to be moved to a service
                    // await session.ChangeClassAsync((CharacterClassType)data.Data).ConfigureAwait(false);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
