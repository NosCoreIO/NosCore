//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ItemGenerationService.Handlers
{
    public class TitleHandler : IUseItemEventHandler
    {
        public bool Condition(Item.Item item) => item.ItemType == ItemType.Title;

        public Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            return requestData.ClientSession.SendPacketAsync(new QnaiPacket
            {
                YesPacket = new GuriPacket
                {
                    Type = GuriPacketType.Title,
                    Argument = (uint)requestData.Data.Item1.ItemInstance.ItemVNum,
                    EntityId = requestData.Data.Item1.Slot
                },
                Question = Game18NConstString.AskAddTitle
            });
        }
    }
}
