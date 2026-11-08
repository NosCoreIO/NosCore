//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyLogService(IDao<FamilyLogDto, long> familyLogDao, IClock clock) : IFamilyLogService
    {
        public const char ArgumentSeparator = '|';
        private const int DataMaxLength = 255;

        public async Task AddAsync(long familyId, FamilyLogType type, params object[] arguments)
        {
            var data = string.Join(ArgumentSeparator, arguments);
            await familyLogDao.TryInsertOrUpdateAsync(new FamilyLogDto
            {
                FamilyId = familyId,
                FamilyLogType = type,
                FamilyLogData = data.Length > DataMaxLength ? data[..DataMaxLength] : data,
                Timestamp = clock.GetCurrentInstant()
            });
        }

        public async Task AddLevelUpsAsync(long familyId, string characterName, byte previousLevel, byte level)
        {
            for (var reached = previousLevel + 1; reached <= level; reached++)
            {
                if (IsAnnouncedLevel(reached))
                {
                    await AddAsync(familyId, FamilyLogType.LevelUp, characterName, reached);
                }
            }
        }

        public static bool IsAnnouncedLevel(int level) => level > 80 || (level > 20 && level % 10 == 0);

        public Task<bool> HasWrittenDailyMessageTodayAsync(long familyId, string characterName)
        {
            var since = clock.GetCurrentInstant() - Duration.FromDays(1);
            var prefix = characterName + ArgumentSeparator;
            var written = familyLogDao.Where(s => s.FamilyId == familyId
                    && s.FamilyLogType == FamilyLogType.DailyMessage && s.Timestamp > since)?
                .Any(s => s.FamilyLogData != null && s.FamilyLogData.StartsWith(prefix, StringComparison.Ordinal)) ?? false;
            return Task.FromResult(written);
        }
    }
}
