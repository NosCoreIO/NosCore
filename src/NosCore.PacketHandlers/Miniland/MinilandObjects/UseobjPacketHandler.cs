//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using System.Linq;
using System.Threading.Tasks;
using UseObjPacket = NosCore.Packets.ClientPackets.Miniland.UseObjPacket;

namespace NosCore.PacketHandlers.Miniland.MinilandObjects
{
    public class UseobjPacketHandler(IMinilandService minilandProvider, IPersonalWarehouseService warehouseService,
        IMinigameRewardService minigameRewards)
        : PacketHandler<UseObjPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(UseObjPacket useobjPacket, ClientSession clientSession)
        {
            var miniland = minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            var minilandObject =
                clientSession.Character.MapInstance.MapDesignObjects.Values.FirstOrDefault(s =>
                    s.Slot == useobjPacket.Slot);
            if (minilandObject?.InventoryItemInstance?.ItemInstance is not { } instance)
            {
                return;
            }

            if (!instance.Item.IsWarehouse)
            {
                var maximums = minigameRewards.GetMaximumScores(instance.Item.MinigameType ?? MinigameType.Quarry);
                var full = false;
                await clientSession.SendPacketAsync(new MloInfoPacket
                {
                    IsOwner = miniland.MapInstanceId == clientSession.Character.MapInstanceId,
                    ObjectVNum = instance.ItemVNum,
                    Slot = (byte)useobjPacket.Slot,
                    MinilandPoints = miniland.MinilandPoint,
                    LawDurability = minilandObject.DurabilityPoint < 1000,
                    IsFull = full,
                    MinigamePoints = maximums.Select((max, level) => new MloInfoPacketSubPacket
                    {
                        MinimumPoints = level == 0 ? 0 : maximums[level - 1] + 1,
                        MaximumPoints = max
                    }).ToArray()
                });
            }
            else
            {
                await warehouseService.OpenAsync(clientSession, WarehouseType.Warehouse);
            }
        }
    }
}
