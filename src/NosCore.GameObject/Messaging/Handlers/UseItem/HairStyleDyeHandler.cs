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
using NosCore.GameObject.Services.EquipmentService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class HairStyleDyeHandler(List<HairStyleDyeDto> dyes)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Magical || item.Effect != ItemEffectType.HairStyleDye)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.IsVehicled)
            {
                return;
            }

            var dye = dyes.FirstOrDefault(d => d.ItemVNum == item.VNum && d.HairStyle == character.HairStyle);
            if (dye == null)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.OnlyWithPunkAndBomb
                });
                return;
            }

            character.HairStyle = dye.DyedHairStyle;
            await session.SendPacketAsync(character.GenerateEq());
            await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
            await character.UseUpOneAsync(evt.InventoryItem);
        }
    }
}
