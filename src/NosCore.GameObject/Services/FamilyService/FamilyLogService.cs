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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public class FamilyLogService(IDao<FamilyLogDto, long> familyLogDao, IClock clock) : IFamilyLogService
    {
        private const int MaxArguments = 4;
        private const int ArgumentMaxLength = 255;

        public async Task AddAsync(long familyId, FamilyLogType type, params object[] arguments)
        {
            if (arguments.Length > MaxArguments)
            {
                throw new ArgumentOutOfRangeException(nameof(arguments), arguments.Length, null);
            }

            await familyLogDao.TryInsertOrUpdateAsync(new FamilyLogDto
            {
                FamilyId = familyId,
                FamilyLogType = type,
                FirstArgument = ArgumentAt(arguments, 0),
                SecondArgument = ArgumentAt(arguments, 1),
                ThirdArgument = ArgumentAt(arguments, 2),
                FourthArgument = ArgumentAt(arguments, 3),
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
            var written = familyLogDao.Where(s => s.FamilyId == familyId
                    && s.FamilyLogType == FamilyLogType.DailyMessage
                    && s.FirstArgument == characterName
                    && s.Timestamp > since)?
                .Any() ?? false;
            return Task.FromResult(written);
        }

        private static string? ArgumentAt(object[] arguments, int index)
        {
            if (index >= arguments.Length)
            {
                return null;
            }

            var value = Convert.ToString(arguments[index], CultureInfo.InvariantCulture) ?? string.Empty;
            return value.Length > ArgumentMaxLength ? value[..ArgumentMaxLength] : value;
        }
    }
}
