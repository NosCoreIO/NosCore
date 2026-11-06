//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ProductionService
{
    public interface IProductionService
    {
        Task OpenAsync(ClientSession session, NpcComponentBundle npc);

        Task OpenScrollAsync(ClientSession session, short scrollVNum);

        Task ShowRecipeAsync(ClientSession session, short itemVNum);

        Task ProduceAsync(ClientSession session, short itemVNum);
    }
}
