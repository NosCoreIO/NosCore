//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.PotionService;
using NosCore.Shared.Enumerations;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class PotionHandler(IPotionCooldownService potionCooldown, IRecoveryBonusService recoveryBonus)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Potion)
            {
                return;
            }

            var session = evt.ClientSession;
            var fullRestore = item.RestoresAllHp || item.RestoresAllMp;
            if (fullRestore && session.Character.MapInstance is { IsPvp: true } or { IsAct4: true })
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CanNotUseInPvp
                });
                return;
            }

            var itemInstance = evt.InventoryItem;
            InventoryItemInstance? left = null;
            var refilledMates = new List<Services.MateService.Mate>();
            var drunk = await session.Character.OnMapAsync(() =>
            {
                var character = session.Character;
                var restoresHp = (item.Hp > 0 || item.RestoresAllHp) && character.Hp < character.MaxHp;
                var restoresMp = (item.Mp > 0 || item.RestoresAllMp) && character.Mp < character.MaxMp;
                if (!character.IsAlive || (!restoresHp && !restoresMp) || !potionCooldown.IsReady(character))
                {
                    return Task.FromResult(false);
                }

                potionCooldown.Start(character);
                left = character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
                character.Hp = item.RestoresAllHp ? character.MaxHp : Math.Min(character.MaxHp, character.Hp + recoveryBonus.Boost(character, item.Hp));
                character.Mp = item.RestoresAllMp ? character.MaxMp : Math.Min(character.MaxMp, character.Mp + recoveryBonus.Boost(character, item.Mp));
                if (fullRestore)
                {
                    foreach (var mate in character.Mates.Values.Where(m => m.IsTeamMember))
                    {
                        if (mate.Entity is not { Hp: > 0 } entity)
                        {
                            continue;
                        }

                        entity.Hp = item.RestoresAllHp ? entity.MaxHp : entity.Hp;
                        entity.Mp = item.RestoresAllMp ? entity.MaxMp : entity.Mp;
                        refilledMates.Add(mate);
                    }
                }

                return Task.FromResult(true);
            });
            if (!drunk)
            {
                return;
            }

            await session.SendPacketAsync(left.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
            await session.SendPacketAsync(session.Character.GenerateStat());
            await session.SendPacketsAsync(refilledMates.Select(m => m.GeneratePst()));
        }
    }
}
