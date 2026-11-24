//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.TransformationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class CostumeScrollHandler(List<CostumeMorphDto> costumeMorphs,
        ITransformationService transformationService, IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var inventoryItem = evt.InventoryItem;
            var item = inventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Special || item.Effect != ItemEffectType.CostumeScroll)
            {
                return;
            }

            var morphs = costumeMorphs.Where(m => m.ItemVNum == item.VNum).Select(m => m.Morph).ToList();
            if (morphs.Count == 0)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.InMonsterForm)
            {
                await transformationService.RemoveMonsterFormAsync(session);
                return;
            }

            if (character.UseSp || character.IsVehicled)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = character.UseSp
                        ? Game18NConstString.CantTrasformInMonsterWhileSpecialist
                        : Game18NConstString.CannotTransformInMonsterInVehicle
                });
                return;
            }

            if (!character.InventoryService.TryConsume(inventoryItem, 1))
            {
                return;
            }

            await session.SendPacketAsync(character.InventoryService.LoadBySlotAndType(inventoryItem.Slot, inventoryItem.Type)
                .GeneratePocketChange(inventoryItem.Type.ToPocketType(), inventoryItem.Slot));
            await transformationService.ChangeMonsterFormAsync(session, morphs[random.Next(0, morphs.Count)]);
        }
    }
}
