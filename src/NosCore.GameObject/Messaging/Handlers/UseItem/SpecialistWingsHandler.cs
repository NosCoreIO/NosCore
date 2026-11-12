//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpecialistWingsHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Special || item.Effect != ItemEffectType.ChangeSpecialistWings)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var sp = character.InventoryService.LoadBySlotAndType((byte)EquipmentType.Sp, NoscorePocketType.Wear);
            if (!character.UseSp || sp == null)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.TransformToChangeWings
                });
                return;
            }

            if (sp.ItemInstance.Upgrade < 1)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.WingsOnlyFromPlusOne
                });
                return;
            }

            var itemInstance = evt.InventoryItem;
            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = character.GenerateUseItem(itemInstance.Type.ToPocketType(), itemInstance.Slot, 1, 0),
                    Question = Game18NConstString.AskChangeWings
                });
                return;
            }

            sp.ItemInstance.Design = (short)item.EffectValue;
            character.MorphDesign = (short)item.EffectValue;
            await character.UseUpOneAsync(itemInstance);
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
            await session.SendPacketAsync(character.GenerateStat());
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.WingsChanged
            });
        }
    }
}
