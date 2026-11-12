//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.BattleService;

public sealed class StaticBuffService(IDao<StaticBuffDto, long> staticBuffDao, IBuffService buffService,
    ICardCatalog cardCatalog, IClock clock) : IStaticBuffService
{
    public void Load(ICharacterEntity character)
    {
        var characterId = character.CharacterId;
        foreach (var row in staticBuffDao.Where(s => s.CharacterId == characterId)?.ToList() ?? [])
        {
            if (row.RemainingTime > 0 && cardCatalog.GetCard(row.CardId) is { } card)
            {
                buffService.RestoreStatic(character, card, cardCatalog.GetCardBCards(row.CardId),
                    Duration.FromSeconds(row.RemainingTime));
            }
        }
    }

    public async Task<bool> SaveAsync(ICharacterEntity character)
    {
        var characterId = character.CharacterId;
        var now = clock.GetCurrentInstant();
        var stored = staticBuffDao.Where(s => s.CharacterId == characterId)?.ToList() ?? [];
        // A buff without an end is put back by whatever grants it, not from here.
        var rows = buffService.GetActiveBuffs(character)
            .Where(s => s.Static && s.ExpiresAt != Instant.MaxValue)
            .Select(s => new StaticBuffDto
            {
                StaticBuffId = stored.FirstOrDefault(o => o.CardId == s.CardId)?.StaticBuffId ?? 0,
                CharacterId = characterId,
                CardId = s.CardId,
                RemainingTime = (int)(s.ExpiresAt - now).TotalSeconds
            })
            .Where(s => s.RemainingTime > 0)
            .ToList();
        var ended = stored.Where(s => rows.All(o => o.StaticBuffId != s.StaticBuffId)).Select(s => s.StaticBuffId).ToList();
        return (ended.Count == 0 || await staticBuffDao.TryDeleteAsync(ended) != null)
            && (rows.Count == 0 || await staticBuffDao.TryInsertOrUpdateAsync(rows));
    }
}
