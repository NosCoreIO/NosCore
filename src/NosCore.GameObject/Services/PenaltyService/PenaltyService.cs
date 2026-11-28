//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Core.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Interaction;

namespace NosCore.GameObject.Services.PenaltyService
{
    // Read from the table on every question: a penalty is given to an account, from any channel,
    // while its character may already be in the world.
    public sealed class PenaltyService(IDao<PenaltyLogDto, int> penaltyLogDao, IDao<CharacterDto, long> characterDao,
        IDao<AccountDto, long> accountDao, IClock clock) : IPenaltyService
    {
        public static readonly Instant Forever = Instant.FromUtc(9999, 1, 1, 0, 0);

        public Task<Duration?> GetMuteLeftAsync(long accountId)
        {
            var end = LatestEnd(accountId, PenaltyType.Muted);
            return Task.FromResult(end == null ? (Duration?)null : end.Value - clock.GetCurrentInstant());
        }

        public Task<bool> IsBannedAsync(long accountId) => IsRunningAsync(accountId, PenaltyType.Banned);

        public Task<bool> IsRunningAsync(long accountId, PenaltyType penalty) =>
            Task.FromResult(LatestEnd(accountId, penalty) != null);

        public async Task<bool> IsRunningForCharacterAsync(long characterId, PenaltyType penalty)
        {
            var character = await characterDao.FirstOrDefaultAsync(c => c.CharacterId == characterId);
            return character != null && LatestEnd(character.AccountId, penalty) != null;
        }

        public async Task<AccountDto?> FindAccountAsync(string? characterName, int serverId)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                return null;
            }

            var character = await characterDao.FirstOrDefaultAsync(c =>
                c.Name == characterName && c.ServerId == serverId && c.State == CharacterState.Active);
            return character == null ? null : await accountDao.FirstOrDefaultAsync(a => a.AccountId == character.AccountId);
        }

        public bool MayPunish(AccountDto moderator, AccountDto target) =>
            target.AccountId != moderator.AccountId && target.Authority < moderator.Authority;

        public Task GiveAsync(long accountId, PenaltyType penalty, Instant end, string reason, string adminName) =>
            penaltyLogDao.TryInsertOrUpdateAsync(new PenaltyLogDto
            {
                AccountId = accountId,
                Penalty = penalty,
                DateStart = clock.GetCurrentInstant(),
                DateEnd = end,
                Reason = reason,
                AdminName = adminName,
            }).OrThrowAsync();

        public async Task<bool> LiftAsync(long accountId, PenaltyType penalty)
        {
            var now = clock.GetCurrentInstant();
            var running = penaltyLogDao
                .Where(s => s.AccountId == accountId && s.Penalty == penalty && s.DateEnd > now)?
                .ToList();
            if (running == null || running.Count == 0)
            {
                return false;
            }

            foreach (var log in running)
            {
                log.DateEnd = now;
            }

            await penaltyLogDao.TryInsertOrUpdateAsync(running).OrThrowAsync();
            return true;
        }

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
