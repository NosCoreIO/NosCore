//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    [UsedImplicitly]
    public sealed class SpeedBoostEndHandler(IBuffService buffService, ICardCatalog cardCatalog, List<ItemDto> items)
    {
        [UsedImplicitly]
        public async Task Handle(BuffExpiredEvent evt)
        {
            var character = evt.Character;
            if (!character.IsVehicled || !cardCatalog.IsGrantedBy(evt.CardId, ItemEffectType.SpeedBooster)
                || items.Find(i => i.VNum == character.VehicleVNum) is not { SpeedBoostEndCardId: > 0 } mount
                || cardCatalog.GetCard(mount.SpeedBoostEndCardId) is not { } card)
            {
                return;
            }

            await buffService.ApplyAsync(character, card, cardCatalog.GetCardBCards(card.CardId), character);
        }
    }
}
