//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations.Buff;
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
    public sealed class PotionHandler(IPotionCooldownService potionCooldown, IRecoveryBonusService recoveryBonus,
        IBuffService buffService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Potion && (item.ItemType != ItemType.Event || item is { Hp: 0, Mp: 0 }))
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
            var character = session.Character;
            var restoresHp = (item.Hp > 0 || item.RestoresAllHp) && character.Hp < character.MaxHp
                && !buffService.HasEffect(character, BCardEffect.TimeCircleSkillsDisableHpRecovery);
            var restoresMp = (item.Mp > 0 || item.RestoresAllMp) && character.Mp < character.MaxMp
                && !buffService.HasEffect(character, BCardEffect.TimeCircleSkillsDisableMpRecovery);
            if (!character.IsAlive || (!restoresHp && !restoresMp) || !potionCooldown.IsReady(character))
            {
                return;
            }

            potionCooldown.Start(character);
            var left = character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            var hp = recoveryBonus.Boost(character, item.Hp);
            var refilledMates = (await character.HealAsync(!restoresHp ? 0 : item.RestoresAllHp ? character.MaxHp : hp,
                !restoresMp ? 0 : item.RestoresAllMp ? character.MaxMp : recoveryBonus.Boost(character, item.Mp),
                fullRestore ? 0 : hp)).ToList();
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

            await session.SendPacketAsync(left.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
            await session.SendPacketsAsync(refilledMates.Select(m => m.GeneratePst()));
        }
    }
}
