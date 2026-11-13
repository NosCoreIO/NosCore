//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;

namespace NosCore.GameObject.Services.PenaltyService
{
    // Read from the table on every question: a penalty is given to an account, from any channel,
    // while its character may already be in the world.
    public sealed class PenaltyService(IDao<PenaltyLogDto, int> penaltyLogDao, IClock clock) : IPenaltyService
    {
        public Task<Duration?> GetMuteLeftAsync(long accountId)
        {
            var end = LatestEnd(accountId, PenaltyType.Muted);
            return Task.FromResult(end == null ? (Duration?)null : end.Value - clock.GetCurrentInstant());
        }

        public Task<bool> IsBannedAsync(long accountId) =>
            Task.FromResult(LatestEnd(accountId, PenaltyType.Banned) != null);

        private Instant? LatestEnd(long accountId, PenaltyType penalty)
        {
            var now = clock.GetCurrentInstant();
            var running = penaltyLogDao
                .Where(s => s.AccountId == accountId && s.Penalty == penalty && s.DateEnd > now)?
                .Select(s => s.DateEnd)
                .ToList();
            return running == null || running.Count == 0 ? null : running.Max();
        }
    }
}
