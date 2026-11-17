//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public sealed class TimeSpaceAttemptService(IDao<CharacterTimeSpaceDto, Guid> characterTimeSpaceDao,
            ILogger<TimeSpaceAttemptService> logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : ITimeSpaceAttemptService
    {
        public const short LoserModeFailures = 2;

        public async Task<short?> RecordFailureAsync(long characterId, short timeSpaceId)
        {
            var attempts = await FindAsync(characterId, timeSpaceId) ?? new CharacterTimeSpaceDto
            {
                CharacterTimeSpaceId = Guid.NewGuid(),
                CharacterId = characterId,
                TimeSpaceId = timeSpaceId
            };
            attempts.FailedAttempts++;
            return await SaveAsync(attempts) ? attempts.FailedAttempts : null;
        }

        public async Task ResetAsync(long characterId, short timeSpaceId)
        {
            if (await FindAsync(characterId, timeSpaceId) is { FailedAttempts: > 0 } attempts)
            {
                attempts.FailedAttempts = 0;
                await SaveAsync(attempts);
            }
        }

        public async Task<bool> OffersLoserModeAsync(long characterId, short timeSpaceId) =>
            await FindAsync(characterId, timeSpaceId) is { FailedAttempts: >= LoserModeFailures };

        private Task<CharacterTimeSpaceDto?> FindAsync(long characterId, short timeSpaceId) =>
            characterTimeSpaceDao.FirstOrDefaultAsync(s => s.CharacterId == characterId && s.TimeSpaceId == timeSpaceId);

        private async Task<bool> SaveAsync(CharacterTimeSpaceDto attempts)
        {
            if (await characterTimeSpaceDao.TryInsertOrUpdateAsync(attempts) != null)
            {
                return true;
            }

            logger.LogError(logLanguage[LogLanguageKey.SAVE_CHARACTER_FAILED], attempts.CharacterId);
            return false;
        }
    }
}
