//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class BandageHandler(IBuffService buffService, ICardCatalog cardCatalog)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var character = evt.ClientSession.Character;
            if (item.Effect != ItemEffectType.Bandage || !character.IsAlive)
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);
            await buffService.InflictCardsAsync(character, character, cardCatalog.GetItemBCards(item.VNum));
        }
    }
}
