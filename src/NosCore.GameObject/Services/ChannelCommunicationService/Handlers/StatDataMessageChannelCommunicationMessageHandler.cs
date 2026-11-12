//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.SkillPointService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using NosCore.Core.I18N;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.SkillService;
using System.Threading.Tasks;
using StatData = NosCore.GameObject.InterChannelCommunication.Messages.StatData;

namespace NosCore.GameObject.Services.ChannelCommunicationService.Handlers
{
    public class StatDataMessageChannelCommunicationMessageHandler(ILogger<StatDataMessageChannelCommunicationMessageHandler> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IOptions<WorldConfiguration> worldConfiguration, ISessionRegistry sessionRegistry,
        IExperienceService experienceService, IJobExperienceService jobExperienceService, IHeroExperienceService heroExperienceService, ISkillPointService skillPointService,
        IGameLanguageLocalizer gameLanguageLocalizer, IItemGenerationService itemProvider, ISkillService skillService, List<ItemDto> items)
        : ChannelCommunicationMessageHandler<StatData>
    {
        public override async Task Handle(StatData data)
        {
            var session = sessionRegistry.GetSession(s => s.Character.Name == data.Character?.Name);

            if (session == null)
            {
                return;
            }

            var character = session.Character;
            switch (data.ActionType)
            {
                case UpdateStatActionType.UpdateLevel:
                    await character.SetLevelAsync((byte)data.Data, experienceService, jobExperienceService, heroExperienceService, skillPointService,
                        sessionRegistry, gameLanguageLocalizer);
                    break;
                case UpdateStatActionType.UpdateJobLevel:
                    await character.SetJobLevelAsync((byte)data.Data, experienceService, jobExperienceService, heroExperienceService, skillPointService);
                    break;
                case UpdateStatActionType.UpdateHeroLevel:
                    await character.SetHeroLevelAsync((byte)data.Data, experienceService, jobExperienceService, heroExperienceService, skillPointService);
                    break;
                case UpdateStatActionType.UpdateReputation:
                    await character.SetReputationAsync(data.Data);
                    break;
                case UpdateStatActionType.UpdateGold:
                    if (character.Gold + data.Data > worldConfiguration.Value.MaxGoldAmount)
                    {
                        return;
                    }

                    await character.SetGoldAsync(data.Data, items);
                    break;
                case UpdateStatActionType.UpdateClass:
                    await session.ChangeClassAsync((CharacterClassType)data.Data, worldConfiguration, experienceService, jobExperienceService,
                        heroExperienceService, skillPointService, itemProvider, skillService);
                    break;
                default:
                    logger.LogError(logLanguage[LogLanguageKey.UNKWNOWN_RECEIVERTYPE]);
                    break;
            }
        }
    }
}
