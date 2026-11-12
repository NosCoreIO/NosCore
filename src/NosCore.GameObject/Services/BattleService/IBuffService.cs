//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Lifecycle for buff/debuff cards attached to an entity. Applying the same CardId twice
// refreshes the duration rather than stacking, which matches how the original client
// renders the UI (one icon per card id).
public interface IBuffService
{
    Task ApplyAsync(IAliveEntity target, CardDto card, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster, int overrideDuration = -1);

    // Convenience wrapper for skill-originated buffs: the skill itself doesn't come with
    // a CardDto, so we use the skill's SkillVnum as the dedup key and the skill's
    // Duration (in centiseconds) as the lifetime. BCards with Type=15 (Damage) are
    // filtered out because damage is applied immediately, not as a lasting buff.
    Task ApplySkillBuffAsync(IAliveEntity target, short skillVnum, short skillDuration, IReadOnlyList<BCardDto> bCards, IAliveEntity? caster);

    // Stays until removed, through death included, and shows in the static buff bar.
    Task ApplyPermanentAsync(ICharacterEntity target, CardDto card, IReadOnlyList<BCardDto> bCards);

    // Shows in the static buff bar with its time left and survives death, until it runs out.
    Task ApplyStaticAsync(ICharacterEntity target, CardDto card, IReadOnlyList<BCardDto> bCards, Duration duration);

    Task InflictCardsAsync(IAliveEntity target, IAliveEntity? caster,
        IReadOnlyList<BCardDto> skillBCards);

    Task RemoveAsync(IAliveEntity target, short cardId);

    // Leaves out buffs that have run out, even if the life tick has not swept them away yet.
    IReadOnlyCollection<BuffInstance> GetActiveBuffs(IAliveEntity target);

    Task ClearAsync(IAliveEntity target);

    bool HasBuff(IAliveEntity target, short cardId);

    // Whether a buff that has not yet run out carries the effect, even if the life tick has
    // not swept the expired ones away yet.
    bool HasEffect(IAliveEntity target, BCardEffect effect);

    // Drops buffs whose ExpiresAt is past `now` and takes their icons down itself.
    Task<IReadOnlyList<BuffInstance>> TickAsync(IAliveEntity target);
}
