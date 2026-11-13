//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Dto;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class ItemPackHandler(IItemGenerationService itemProvider,
        IDao<RollGeneratedItemDto, short> rollGeneratedItemDao)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            if (source.ItemInstance.Item.Effect != ItemEffectType.ItemPack)
            {
                return;
            }

            var packVNum = source.ItemInstance.ItemVNum;
            var contents = (rollGeneratedItemDao.Where(r => r.OriginalItemVNum == packVNum) ?? [])
                .OrderBy(r => r.RollGeneratedItemId)
                .Select(r => itemProvider.Create(r.ItemGeneratedVNum, r.ItemGeneratedAmount, 0, r.ItemGeneratedUpgrade))
                .ToList();
            if (contents.Count == 0)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var inventory = character.InventoryService;
            if (!inventory.HasRoomFor(contents))
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            if (!inventory.TryConsume(source, 1))
            {
                return;
            }

            var granted = contents
                .Select(item => (Item: item, Slots: inventory.AddItemToPocket(InventoryItemInstance.Create(item, character.CharacterId)) ?? []))
                .ToList();
            foreach (var (item, slots) in granted)
            {
                foreach (var added in slots)
                {
                    await session.SendPacketAsync(added.GeneratePocketChange(added.Type.ToPocketType(), added.Slot));
                }

                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, item.ItemVNum.ToString(), item.Amount));
            }

            await session.SendPacketAsync(inventory.LoadBySlotAndType(source.Slot, source.Type)
                .GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
        }
    }
}
