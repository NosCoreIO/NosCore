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
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.MovementService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpeedBoosterHandler(IBuffService buffService, ICardCatalog cardCatalog,
        ISpeedCalculationService speedCalculationService, List<ItemDto> items, IRandomProvider random)
    {
        private const int TeleportAttempts = 100;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var session = evt.ClientSession;
            var character = session.Character;
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.SpeedBooster || !character.IsVehicled
                || items.Find(i => i.VNum == character.VehicleVNum) is not { } mount
                || mount is { SpeedBoost: 0, SpeedBoostRestoresPerLevel: 0, SpeedBoostRemovesDebuffsUpToLevel: 0, SpeedBoostTeleportsRandomly: false, SpeedBoostDashCells: 0 }
                || cardCatalog.GetItemCard(evt.InventoryItem.ItemInstance.ItemVNum) is not { } card
                || buffService.HasBuff(character, card.CardId))
            {
                return;
            }

            var destination = mount.SpeedBoostTeleportsRandomly ? character.MapInstance.Map.RandomWalkableCell(random, TeleportAttempts) : null;
            if (mount.SpeedBoostTeleportsRandomly && destination is null)
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);
            await character.MapInstance.SendPacketAsync(character.GenerateEff(evt.InventoryItem.ItemInstance.Item.EffectValue));

            if (mount.SpeedBoost > 0)
            {
                await buffService.ApplyAsync(character, card, cardCatalog.GetCardBCards(card.CardId), character,
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

            if (mount.SpeedBoostDashCells > 0)
            {
                await DashAsync(character, mount.SpeedBoostDashCells);
            }

            if (destination is { } cell)
            {
                await character.TeleportAsync(cell.X, cell.Y);
                await character.MapInstance.SendPacketAsync(character.GenerateEff(mount.SpeedBoostTeleportEffect));
            }
        }

        private static async Task DashAsync(PlayerComponentBundle character, int cells)
        {
            var (stepX, stepY) = Facing.Step(character.Direction);
            var (x, y) = ForcedMovement.Destination(character.PositionX, character.PositionY,
                (short)(character.PositionX + stepX * cells), (short)(character.PositionY + stepY * cells),
                cells, stepX, stepY, 0, character.MapInstance.Map.IsWalkable);
            if (x != character.PositionX || y != character.PositionY)
            {
                await character.TeleportAsync(x, y);
            }
        }
    }
}
