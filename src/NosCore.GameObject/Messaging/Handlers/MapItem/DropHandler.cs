//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Wolverine;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    [UsedImplicitly]
    public sealed class DropHandler(IMessageBus messageBus)
    {
        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            var item = evt.ItemInstance;
            if (item == null || item.Item.ItemType == ItemType.Map)
            {
                return;
            }

            var session = evt.ClientSession;
            var visualId = evt.VisualId;
            var amount = evt.Amount;
            var mapInstance = session.Character.MapInstance;
            if (!session.Character.InventoryService.HasRoomFor(item))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
                return;
            }

            if (!mapInstance.TryRemoveMapItem(visualId))
            {
                return;
            }

            var pickedUp = (IItemInstance)item.Clone();
            pickedUp.Id = Guid.NewGuid();
            var itemInstance = InventoryItemInstance.Create(pickedUp, session.Character.CharacterId);
            var added = session.Character.InventoryService.AddItemToPocket(itemInstance) ?? [];
            var pocketChange = added.GeneratePocketChange();

            if (pocketChange != null)
            {
                var inv = added[0];
                await session.SendPacketAsync(pocketChange);
                await session.SendPacketAsync(session.Character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, inv.ItemInstance.ItemVNum.ToString(), amount));
                await session.SendPacketAsync(session.Character.GenerateIcon(1, inv.ItemInstance.ItemVNum));
                await mapInstance.SendPacketAsync(session.Character.GenerateGet(visualId));

                if (mapInstance.MapInstanceType == MapInstanceType.LodInstance)
                {
                    await mapInstance.SendPacketAsync(session.Character.GenerateSayi2(SayColorType.Yellow, Game18NConstString.CharacterHasReceivedItem, 13,
                        $"{session.Character.Name} {inv.ItemInstance.Item.VNum}"));
                }

                await messageBus.PublishAsync(new ItemsReceivedEvent(session.Character, inv.ItemInstance.ItemVNum, amount));
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
