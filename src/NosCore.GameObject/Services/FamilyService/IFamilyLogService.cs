//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using NosCore.Data.Enumerations.Family;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.FamilyService
{
    public interface IFamilyLogService
    {
        Task AddAsync(long familyId, FamilyLogType type, params object[] arguments);

        Task AddLevelUpsAsync(long familyId, string characterName, byte previousLevel, byte level);

        Task<bool> HasWrittenDailyMessageTodayAsync(long familyId, string characterName);
    }
}
