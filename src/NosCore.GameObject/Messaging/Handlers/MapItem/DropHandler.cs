//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class DropHandler
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            var mapItem = evt.MapItem;
            if (mapItem.ItemInstance!.Item.ItemType == ItemType.Map || mapItem.VNum == 1046)
            {
                return;
            }

            var session = evt.ClientSession;
            var visualId = mapItem.VisualId;
            var amount = mapItem.Amount;
            var itemInstance = InventoryItemInstance.Create(mapItem.ItemInstance, session.Character.CharacterId);
            var inv = session.Character.InventoryService.AddItemToPocket(itemInstance)?.FirstOrDefault();

            if (inv != null)
            {
                await session.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
                session.Character.MapInstance.TryRemoveMapItem(visualId);
                await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateGet(visualId));
                if (evt.Packet.PickerType == VisualType.Npc)
                {
                    await session.SendPacketAsync(session.Character.GenerateIcon(1, inv.ItemInstance.ItemVNum));
                }

                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ReceivedThisItem,
                    ArgumentType = 2,
                    Game18NArguments = { inv.ItemInstance.ItemVNum.ToString(), amount }
                });

                if (session.Character.MapInstance.MapInstanceType == MapInstanceType.LodInstance)
                {
                    await session.Character.MapInstance.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = session.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.CharacterHasReceivedItem,
                        ArgumentType = 13,
                        Game18NArguments = { $"{session.Character.Name} {inv.ItemInstance.Item.VNum}" }
                    });
                }
            }
            else
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
            }
        }
    }
}
