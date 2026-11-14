//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 3001 {scriptId}: a story chapter picked from the NPC's "Get Quest" list.
    [UsedImplicitly]
    public sealed class TakeScriptOfferHandler(IQuestService questService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GetQuestFromList;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not INonPlayableEntity { NpcMonster: { } npc } || packet.Type is not { } scriptId
                || scriptId is < byte.MinValue or > byte.MaxValue)
            {
                return Task.CompletedTask;
            }

            return questService.TakeScriptOfferAsync(session.Character, npc.NpcMonsterVNum, (byte)scriptId);
        }
    }
}
