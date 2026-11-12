//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
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
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Packets.ServerPackets.Entities;
using NosCore.Shared.Enumerations;

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

            var itemInstance = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
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
                await RestoreAsync(character, character.Level * mount.SpeedBoostRestoresPerLevel);
            }

            if (mount.SpeedBoostTeleportsRandomly)
            {
                await TeleportRandomlyAsync(character);
            }
        }

        private static async Task RestoreAsync(PlayerComponentBundle character, int amount)
        {
            var hp = Math.Min(character.MaxHp - character.Hp, amount);
            character.Hp += Math.Max(hp, 0);
            character.Mp = Math.Min(character.MaxMp, character.Mp + amount);
            if (hp > 0)
            {
                await character.MapInstance.SendPacketAsync(new RcPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.VisualId,
                    HealAmount = hp,
                    Unknown = 0
                });
            }

            await character.SendPacketAsync(character.GenerateStat());
        }

        private async Task TeleportRandomlyAsync(PlayerComponentBundle character)
        {
            var map = character.MapInstance.Map;
            for (var i = 0; i < SpeedBooster.TeleportAttempts; i++)
            {
                var x = (short)random.Next(0, map.Width);
                var y = (short)random.Next(0, map.Height);
                if (!map.IsWalkable(x, y))
                {
                    continue;
                }

                character.PositionX = x;
                character.PositionY = y;
                await character.MapInstance.SendPacketAsync(new TpPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.VisualId,
                    X = x,
                    Y = y,
                });
                await character.MapInstance.SendPacketAsync(character.GenerateEff(SpeedBooster.TeleportEffect));
                return;
            }
        }
    }
}
