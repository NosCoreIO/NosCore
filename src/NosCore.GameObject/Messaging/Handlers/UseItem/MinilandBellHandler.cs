//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class MinilandBellHandler(IMinilandService minilandProvider, IMapChangeService mapChangeService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.Teleport || item.EffectValue != 2)
            {
                return;
            }

            var session = evt.ClientSession;
            var itemInstance = evt.InventoryItem;
            var packet = evt.Packet;

            var character = session.Character;
            if (character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotBeUsedHere
                });
                return;
            }

            character = session.Character;
            if (character.IsVehicled)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.OnlyPotionInVehicle
                });
                return;
            }

            if (packet.Mode == 0)
            {
                character = session.Character;
                await session.SendPacketAsync(new DelayPacket
                {
                    Delay = 5000,
                    Type = DelayPacketType.ItemInUse,
                    Packet = character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, 2, 0)
                });
                return;
            }

            character = session.Character;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            var characterId = character.CharacterId;
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            var miniland = minilandProvider.GetMiniland(characterId);
            await mapChangeService.ChangeMapInstanceAsync(session, miniland.MapInstanceId, 5, 8);
        }
    }
}
