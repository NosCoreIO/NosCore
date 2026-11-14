//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Services.TransformationService;

public sealed class SpecialistWingsService(IBuffService buffService, ICardCatalog cardCatalog)
    : ISpecialistWingsService, ISingletonService
{
    public async Task RefreshAsync(PlayerComponentBundle character)
    {
        var wanted = character.UseSp ? cardCatalog.GetSpecialistWingsCard(character.MorphDesign) : null;
        var buffs = buffService.GetActiveBuffs(character);
        foreach (var stale in buffs
                     .Where(b => b.CardId != wanted?.CardId && cardCatalog.IsGrantedBy(b.CardId, ItemEffectType.ChangeSpecialistWings))
                     .Select(b => b.CardId).ToList())
        {
            await buffService.RemoveAsync(character, stale);
        }

        if (wanted != null && buffs.All(b => b.CardId != wanted.CardId))
        {
            await buffService.ApplyPermanentAsync(character, wanted, cardCatalog.GetCardBCards(wanted.CardId));
        }
    }
}
