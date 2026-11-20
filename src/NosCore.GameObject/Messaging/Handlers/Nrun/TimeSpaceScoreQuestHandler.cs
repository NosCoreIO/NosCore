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
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 3002 {option}: one of the score quest buttons, one per level range.
    [UsedImplicitly]
    public sealed class TimeSpaceScoreQuestHandler(IQuestService questService, List<QuestDto> quests) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GetQuestHigherTimeSpaceScore;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (packet.Type is not { } option
                || quests.FirstOrDefault(q => q.OfferedBy == Runner && q.OfferOption == option) is not { } quest)
            {
                return Task.CompletedTask;
            }

            return questService.AddQuestAsync(session.Character, QuestActionType.Achieve, quest.QuestId);
        }
    }
}
