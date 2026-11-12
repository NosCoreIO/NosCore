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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpeedBoosterHandler(IBuffService buffService, ICardCatalog cardCatalog,
        ISpeedCalculationService speedCalculationService, List<ItemDto> items, IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var session = evt.ClientSession;
            var character = session.Character;
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.SpeedBooster || !character.IsVehicled
                || items.Find(i => i.VNum == character.VehicleVNum) is not { } mount
                || mount is { SpeedBoost: 0, SpeedBoostRestoresPerLevel: 0, SpeedBoostRemovesDebuffsUpToLevel: 0, SpeedBoostTeleportsRandomly: false }
                || buffService.HasBuff(character, SpeedBooster.CardId)
                || cardCatalog.GetCard(SpeedBooster.CardId) is not { } card)
            {
                return;
            }

            var destination = mount.SpeedBoostTeleportsRandomly ? character.MapInstance.Map.RandomWalkableCell(random, SpeedBooster.TeleportAttempts) : null;
            if (mount.SpeedBoostTeleportsRandomly && destination is null)
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);
            await character.MapInstance.SendPacketAsync(character.GenerateEff(SpeedBooster.Effect));

            if (mount.SpeedBoost > 0)
            {
                await buffService.ApplyAsync(character, card, cardCatalog.GetCardBCards(SpeedBooster.CardId), character,
                    mount.SpeedBoostDuration * 1000);
                character.Speed = speedCalculationService.CalculateSpeed(character);
                await session.SendPacketAsync(character.GenerateCond());
            }

            if (mount.SpeedBoostRemovesDebuffsUpToLevel > 0)
            {
                await buffService.RemoveBadAsync(character, mount.SpeedBoostRemovesDebuffsUpToLevel);
            }

            if (mount.SpeedBoostRestoresPerLevel > 0)
            {
                var amount = character.Level * mount.SpeedBoostRestoresPerLevel;
                await character.HealAsync(amount, amount);
            }

            if (destination is { } cell)
            {
                await character.TeleportAsync(cell.X, cell.Y);
                await character.MapInstance.SendPacketAsync(character.GenerateEff(SpeedBooster.TeleportEffect));
            }
        }
    }
}
