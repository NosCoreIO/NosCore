using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using StatData = NosCore.GameObject.InterChannelCommunication.Messages.StatData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class StatDataMessageChannelCommunicationMessageHandler(ILogger logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration, ISessionRegistry sessionRegistry,
        IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService) : ChannelCommunicationMessageHandler<StatData>
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
                    await session.SetJobLevelAsync((byte)data.Data, experienceService, jobExperienceService, heroExperienceService);
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    await session.SetHeroLevelAsync((byte)data.Data, experienceService, jobExperienceService, heroExperienceService);
                    break;
                case UpdateStatActionType.UpdateReputation:
                    await session.SetReputationAsync(data.Data);
                    break;
                case UpdateStatActionType.UpdateGold:
                    if (session.Gold + data.Data > worldConfiguration.Value.MaxGoldAmount)
                    {
                        return;
                    }

                    await session.SetGoldAsync(data.Data);
                    break;
                case UpdateStatActionType.UpdateClass:
                    await session.ChangeClassAsync((CharacterClassType)data.Data, worldConfiguration, experienceService, jobExperienceService, heroExperienceService);
                    break;
                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }
        }
    }
}
