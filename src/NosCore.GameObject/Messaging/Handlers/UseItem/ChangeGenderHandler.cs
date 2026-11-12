//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class ChangeGenderHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.ChangeGender)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;

            if (character.IsVehicled)
            {
                return;
            }

            if (character.InventoryService.Values.Any(v => v.Type == NoscorePocketType.Wear))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.RemoveEquipment
                });
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = character.GenerateUseItem(evt.InventoryItem.Type.ToPocketType(), evt.InventoryItem.Slot, 1, 0),
                    Question = Game18NConstString.AskChangeGender
                });
                return;
            }

            character.Gender = character.Gender == GenderType.Female ? GenderType.Male : GenderType.Female;

            await session.SendPacketAsync(character.GenerateEq());
            await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
            await character.MapInstance.SendPacketAsync(character.GenerateEff(196));

            await character.UseUpOneAsync(evt.InventoryItem);
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.GenderChanged
            });
        }
    }
}
