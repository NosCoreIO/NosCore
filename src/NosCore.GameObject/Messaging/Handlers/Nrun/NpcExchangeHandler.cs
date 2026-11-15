//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//


using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcExchangeService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // An NPC dialog choice that hands over an item for materials, as recorded for its runner.
    public abstract class NpcExchangeHandler(INpcExchangeService npcExchangeService) : INrunEventHandler
    {
        public abstract NrunRunnerType Runner { get; }

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            npcExchangeService.ExchangeAsync(session, Runner);
    }
}
