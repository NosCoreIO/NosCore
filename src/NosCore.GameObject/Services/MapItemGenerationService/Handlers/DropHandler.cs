//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class DropEventHandler : IGetMapItemEventHandler
    {
        public bool Condition(MapItemComponentBundle item)
        {
            return (item.ItemInstance!.Item.ItemType != ItemType.Map) && (item.VNum != 1046);
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItemComponentBundle, GetPacket>> requestData)
        {
            var mapItem = requestData.Data.Item1;
            var visualId = mapItem.VisualId;
            var amount = mapItem.Amount;
            var iteminstance = InventoryItemInstance.Create(mapItem.ItemInstance!,
                requestData.ClientSession.Character.CharacterId);
            var inv = requestData.ClientSession.Character.InventoryService.AddItemToPocket(iteminstance)?
                .FirstOrDefault();

            if (inv != null)
            {
                await requestData.ClientSession.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot));
                requestData.ClientSession.Character.MapInstance.TryRemoveMapItem(visualId);
                await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateGet(visualId));
                if (requestData.Data.Item2.PickerType == VisualType.Npc)
                {
                    await requestData.ClientSession.SendPacketAsync(
                        requestData.ClientSession.Character.GenerateIcon(1, inv.ItemInstance.ItemVNum));
                }

                await requestData.ClientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = requestData.ClientSession.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ReceivedThisItem,
                    ArgumentType = 2,
                    Game18NArguments = { inv.ItemInstance.ItemVNum.ToString(), amount }
                });

                if (requestData.ClientSession.Character.MapInstance.MapInstanceType == MapInstanceType.LodInstance)
                {
                    await requestData.ClientSession.Character.MapInstance.SendPacketAsync(new Sayi2Packet
                    {
                        VisualType = VisualType.Player,
                        VisualId = requestData.ClientSession.Character.CharacterId,
                        Type = SayColorType.Yellow,
                        Message = Game18NConstString.CharacterHasReceivedItem,
                        ArgumentType = 13,
                        Game18NArguments = { $"{requestData.ClientSession.Character.Name} {inv.ItemInstance.Item.VNum}" }
                    });
                }
            }
            else
            {
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotEnoughSpace
                });
            }
        }
    }
}
