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

// BCard type 25/11 and 25/12: FirstData is the percentage, SecondData the card id. The columns
// tell them apart - 32 distinct values on one side against 780 on the other, and an id does not
// repeat 717 times.
//
// Takes a target rather than a skill because the BCard does not say who receives the card; on a
// blow that has landed the entity that took the damage is the one it goes on.
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
