//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Core.Configuration;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.SpecialistCardService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpRechargerHandler(IOptions<WorldConfiguration> worldConfiguration, IBuffService buffService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Special
                || item.Effect < ItemEffectType.DroppedSpRecharger
                || item.Effect > ItemEffectType.CraftedSpRecharger)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;

            if (character.SpAdditionPoint < worldConfiguration.Value.MaxAdditionalSpPoints)
            {
                var itemInstance = evt.InventoryItem;
                await character.UseUpOneAsync(itemInstance);

                character = session.Character;
                character.AddAdditionalSpPoints(itemInstance.ItemInstance.Item.EffectValue, worldConfiguration);
                await session.SendPacketAsync(character.GenerateSpPoint(worldConfiguration));
                if (item.RemovesDebuffsUpToLevel > 0)
                {
                    await buffService.RemoveBadAsync(character, item.RemovesDebuffsUpToLevel);
                }

                if (item.RestoresPercent > 0)
                {
                    await character.HealAsync(character.MaxHp * item.RestoresPercent / 100, character.MaxMp * item.RestoresPercent / 100);
                }
            }
            else
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CannotBeUsedExceedsCapacity
                });
            }
        }
    }
}
