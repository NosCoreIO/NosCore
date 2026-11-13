//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyContributionService(IFamilyLogService familyLogService, IFamilyHub familyHub,
        IPubSubHub pubSubHub) : IFamilyContributionService
    {
        private const int ExperiencePerLevel = 20;

        private static bool IsRewardedLevel(int level) => level > 20 && level % 10 == 0;

        public async Task CharacterLevelledUpAsync(long familyId, long characterId, string characterName,
            byte previousLevel, byte level)
        {
            await familyLogService.AddLevelUpsAsync(familyId, characterName, previousLevel, level);

            for (var reached = previousLevel + 1; reached <= level; reached++)
            {
                if (!IsRewardedLevel(reached))
                {
                    continue;
                }

                var experience = ExperiencePerLevel * reached;
                var gain = await familyHub.AddExperienceAsync(familyId, characterId, experience);
                if (gain == null)
                {
                    return;
                }

                await familyLogService.AddAsync(familyId, FamilyLogType.FamilyXp, characterName, experience);
                await AnnounceAsync(familyId, gain);
            }
        }

        public async Task DailyMessageWrittenAsync(long familyId)
        {
            var gain = await familyHub.AdvanceQuestsAsync(familyId, FamilyQuestObjective.DailyMessage, 1);
            await AnnounceAsync(familyId, gain ?? new FamilyExperienceGain(0, false));
        }

        private async Task AnnounceAsync(long familyId, FamilyExperienceGain gain)
        {
            if (gain.LeveledUp)
            {
                await familyLogService.AddAsync(familyId, FamilyLogType.FamilyLevelUp, gain.FamilyLevel);
            }

            await pubSubHub.SendMessageAsync(new FamilyChangedData
            {
                FamilyId = familyId,
                ReachedFamilyLevel = gain.LeveledUp ? gain.FamilyLevel : null,
                CompletedQuestIds = [.. gain.CompletedQuestIds]
            });
        }
    }
}
