//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Warehouse;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Warehouse
{
    public class FStashEndPackettHandler : PacketHandler<FStashEndPacket>, IWorldPacketHandler
    {
        public override Task ExecuteAsync(FStashEndPacket packet, ClientSession clientSession)
        {
            if (clientSession.TryGetCharacter(out var character))
            {
                character.CloseWarehouse(WarehouseType.FamilyWareHouse);
            }

            return Task.CompletedTask;
        }
    }
}
