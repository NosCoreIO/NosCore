//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 2002 {mapId}: touching a soulstone pulls the player into the room behind it, anywhere in it.
    [UsedImplicitly]
    public sealed class SoulstoneHandler(IMapChangeService mapChangeService, IMapInstanceAccessorService mapInstances,
        INpcTalkButtonCatalog npcTalkButtons, IRandomProvider random) : INrunEventHandler
    {
        private const int CellAttempts = 100;

        public NrunRunnerType Runner => NrunRunnerType.GoToSPStone;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (packet.Type is not { } mapId || target is not NpcComponentBundle npc || !npcTalkButtons.Offers(npc, Runner, mapId)
                || mapInstances.GetBaseMapById(mapId)?.Map.RandomWalkableCell(random, CellAttempts) is not { } cell)
            {
                return Task.CompletedTask;
            }

            return mapChangeService.ChangeMapAsync(session, mapId, cell.X, cell.Y);
        }
    }
}
