//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ProductionService;
using NosCore.Packets.ClientPackets.Shops;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Shops
{
    public class PdtsePacketHandler(IProductionService productionService) : PacketHandler<PdtsePacket>, IWorldPacketHandler
    {
        private const byte Produce = 0;
        private const byte ShowRecipe = 1;

        public override Task ExecuteAsync(PdtsePacket packet, ClientSession session)
        {
            return packet.Type switch
            {
                Produce => productionService.ProduceAsync(session, packet.VNum),
                ShowRecipe => productionService.ShowRecipeAsync(session, packet.VNum),
                _ => Task.CompletedTask
            };
        }
    }
}
