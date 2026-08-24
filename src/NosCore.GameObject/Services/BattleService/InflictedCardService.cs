//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.BattleService;

/// <summary>
/// Type 25 of <c>BCard.dat</c>, subtypes 11 and 12:
///
///     11: Has a %s%% probability of causing [%s].
///     12: There is a %s%% chance that %s will be removed.
///
/// <c>FirstData</c> is the percentage and <c>SecondData</c> is the id of the card. The names
/// settle it beyond doubt: Star Attack declares 60% of card 7 <i>Blackout</i>, Hit of Rage 2% of
/// card 4 <i>Anger</i>, Blood Oath 100% of card 17 <i>Blood Oath</i>. 1340 of the 1341 ids a skill
/// names exist in <c>Card.dat</c>.
///
/// Reading it the other way round is easy and quiet: with 2759 cards spread over ids 0 to 4440,
/// <c>FirstData</c> is also a valid card id 1278 times out of 1341. What tells them apart is not
/// whether the number exists but the shape of the two columns - 32 distinct values on one side
/// and 780 on the other. An id does not repeat 717 times; a probability does.
/// </summary>
/// <remarks>
/// WHO RECEIVES IT is not in the BCard, and this is why the service takes a target rather than a
/// skill. Battle Cry declares "100% of card Battle Cry" and is a self-buff; Suppress declares its
/// card with exactly the same structure and is a debuff on the enemy. The difference lives in the
/// skill's TARGET section, which the file writes as bare numbers with no sentence explaining
/// them - so the files cannot answer it.
///
/// This runs on a blow that has landed, where the question does not arise: the entity that took
/// the damage is the one the card goes on. Skills that damage nobody never reach this path, so
/// self-buffs are outside it by construction rather than by omission. That is 704 of the 1341
/// declarations, the ones whose TargetType is 0.
/// </remarks>
public sealed class InflictedCardService(
    ICardCatalog cardCatalog,
    IBuffService buffService,
    IRandomProvider randomProvider) : IInflictedCardService, ISingletonService
{
    public async Task InflictAsync(IAliveEntity target, IAliveEntity? caster,
        IReadOnlyList<BCardDto> skillBCards)
    {
        for (var i = 0; i < skillBCards.Count; i++)
        {
            var bCard = skillBCards[i];
            if ((BCardType.CardType)bCard.Type != BCardType.CardType.Buff)
            {
                continue;
            }

            var subType = (AdditionalTypes.Buff)bCard.SubType;
            if (subType is not (AdditionalTypes.Buff.ChanceCausing or AdditionalTypes.Buff.ChanceRemoving))
            {
                continue;
            }

            if (!Rolls(bCard.FirstData))
            {
                continue;
            }

            var cardId = (short)bCard.SecondData;
            if (subType == AdditionalTypes.Buff.ChanceRemoving)
            {
                await buffService.RemoveAsync(target, cardId).ConfigureAwait(false);
                continue;
            }

            // One of the 1341 declarations names a card that is not in the file. It is skipped
            // rather than thrown on: a single bad row in the client's data must not take a blow
            // down with it.
            var card = cardCatalog.GetCard(cardId);
            if (card == null)
            {
                continue;
            }

            await buffService
                .ApplyAsync(target, card, cardCatalog.GetCardBCards(cardId), caster)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The roll. <c>Next(0, 100)</c> yields 0 to 99, so "less than" makes 100 always succeed and 0
    /// never - which is what the file means, and 717 of the 1341 declarations say 100.
    /// </summary>
    private bool Rolls(int percent) => percent > 0 && randomProvider.Next(0, 100) < percent;
}
