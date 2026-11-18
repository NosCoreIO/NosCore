//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;

namespace NosCore.GameObject.Services.BattleService;

public sealed class LowLevelBuffService(IBuffService buffService, ICardCatalog cardCatalog) : ILowLevelBuffService
{
    public const byte LastLevel = 80;

    public async Task RefreshAsync(PlayerComponentBundle character)
    {
        if (cardCatalog.GetTriggeredCard(CardTrigger.LowLevelCharacter) is not { } card)
        {
            return;
        }

        var hasBuff = buffService.HasBuff(character, card.CardId);
        if (character.Level > LastLevel)
        {
            if (hasBuff)
            {
                await buffService.RemoveAsync(character, card.CardId);
            }

            return;
        }

        if (!hasBuff && character.IsAlive)
        {
            await buffService.ApplyAsync(character, card, cardCatalog.GetCardBCards(card.CardId), character);
        }
    }

    public Task RenewAsync(PlayerComponentBundle character, short cardId) =>
        cardCatalog.GetTriggeredCard(CardTrigger.LowLevelCharacter)?.CardId == cardId
            ? RefreshAsync(character)
            : Task.CompletedTask;
}
