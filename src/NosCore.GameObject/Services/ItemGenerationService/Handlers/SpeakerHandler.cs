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
    public class SpeakerHandler : IUseItemEventHandler
    {
        public bool Condition(Item.Item item) =>
            item.ItemType == ItemType.Magical && item.Effect == ItemEffectType.Speaker;

        public async Task ExecuteAsync(RequestData<Tuple<InventoryItemInstance, UseItemPacket>> requestData)
        {
            await requestData.ClientSession.SendPacketAsync(new GuriPacket
            {
                Type = GuriPacketType.TextInput,
                Argument = 3,
                SecondArgument = 1,
                EntityId = requestData.Data.Item1.Slot
            });
        }
    }
}
