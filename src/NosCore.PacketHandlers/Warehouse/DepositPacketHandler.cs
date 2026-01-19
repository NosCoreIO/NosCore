//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.WebApi;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.InterChannelCommunication.Hubs.WarehouseHub;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Warehouse;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Warehouse
{
    public class DepositPacketHandler(IWarehouseHub warehouseHttpClient) : PacketHandler<DepositPacket>,
        IWorldPacketHandler
    {
        public override Task ExecuteAsync(DepositPacket depositPacket, ClientSession clientSession)
        {
#pragma warning disable CS0612 //remove the pragma when the actual itemInstance is fetched
            IItemInstance itemInstance = new ItemInstance();
#pragma warning restore CS0612
            short slot = 0;
            return warehouseHttpClient.AddWarehouseItemAsync(new WareHouseDepositRequest
            {

                OwnerId = clientSession.Character.CharacterId,
                WarehouseType = WarehouseType.Warehouse,
                ItemInstance = itemInstance.Adapt<ItemInstanceDto>(),
                Slot = slot

            });
        }
    }
}
