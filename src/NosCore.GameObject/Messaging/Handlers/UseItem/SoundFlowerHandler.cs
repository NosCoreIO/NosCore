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
using NosCore.GameObject.Services.QuestService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // A flower is picked up rather than carried; this covers one that reached a bag anyway.
    [UsedImplicitly]
    public sealed class SoundFlowerHandler(ISoundFlowerService soundFlowerService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.SoundFlower)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            if (await soundFlowerService.StartFlowerQuestAsync(character))
            {
                await character.UseUpOneAsync(evt.InventoryItem);
            }
        }
    }
}
