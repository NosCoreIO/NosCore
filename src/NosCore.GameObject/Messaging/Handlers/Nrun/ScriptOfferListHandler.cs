//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 3000: the "Get Quest" button, which opens the list of story chapters the NPC offers.
    [UsedImplicitly]
    public sealed class ScriptOfferListHandler(List<ScriptOfferDto> scriptOffers) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GetQuestListFromNPC;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (target is not INonPlayableEntity { NpcMonster: { } npc } entity
                || scriptOffers.FirstOrDefault(o => o.NpcVNum == npc.NpcMonsterVNum) is not { } offer)
            {
                return Task.CompletedTask;
            }

            return session.SendPacketAsync(entity.GenerateNpcReq(offer.DialogId));
        }
    }
}
