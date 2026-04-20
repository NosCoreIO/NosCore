//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
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

    Task RemoveAsync(IAliveEntity target, short cardId);

    IReadOnlyCollection<BuffInstance> GetActiveBuffs(IAliveEntity target);

    bool HasBuff(IAliveEntity target, short cardId);

    // Drops buffs whose ExpiresAt is past `now`. Called from a timer; returning the list
    // lets handlers react (e.g. send BfePacket) without re-querying.
    Task<IReadOnlyList<BuffInstance>> TickAsync(IAliveEntity target);
}
