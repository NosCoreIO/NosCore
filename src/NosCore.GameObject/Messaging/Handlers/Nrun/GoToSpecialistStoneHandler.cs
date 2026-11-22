//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 2002 {timeSpaceId}: the dialog button that takes the player to a specialist card's
    // time-space stone.
    [UsedImplicitly]
    public sealed class GoToSpecialistStoneHandler(IMapChangeService mapChangeService,
        List<ScriptedInstanceDto> scriptedInstances, INpcTalkButtonCatalog npcTalkButtons) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GoToSPStone;

        public bool RequiresNpc => true;

        public async Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (packet.Type is not { } timeSpaceId || target is not NpcComponentBundle npc
                || !npcTalkButtons.Offers(npc, Runner, timeSpaceId)
                || scriptedInstances.FirstOrDefault(s => s.Type == ScriptedInstanceType.TimeSpace
                    && s.TimeSpaceId == timeSpaceId) is not { } stone)
            {
                return;
            }

            await mapChangeService.ChangeMapAsync(session, stone.MapId, stone.PositionX, stone.PositionY);
        }
    }
}
