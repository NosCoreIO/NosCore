using System.Threading.Tasks;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.ShipService;

public interface IShipService
{
    Task InitializeAsync();
    Task BoardAsync(ClientSession session, NpcComponentBundle npc, NrunRunnerType runner, short option);
    Task CancelAsync(ClientSession session, NpcComponentBundle npc, NrunRunnerType runner, short option);
    Task DepartAsync();
}
