//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 1000 {timeSpaceId}: the "Time-Space Challenge" of a held quest's dialog enters the time-space
    // the quest asks the player to complete, from where the player stands.
    [UsedImplicitly]
    public sealed class TimeSpaceChallengeHandler(ITimeSpaceEntryService timeSpaceEntryService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GoToXTimeSpace;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            var character = session.Character;
            if (packet.Type is not { } timeSpaceId
                || character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance
                || !character.Quests.Values.Any(q => q.CompletedOn == null && q.ObjectivesCompletedOn == null
                    && q.Quest.QuestType == QuestType.TimesSpace && q.Quest.QuestObjectives.Any(o => o.FirstData == timeSpaceId)))
            {
                return Task.CompletedTask;
            }

            return timeSpaceEntryService.EnterAsync(session, new ScriptedInstance
            {
                MapId = character.MapId,
                PositionX = character.PositionX,
                PositionY = character.PositionY,
                Type = ScriptedInstanceType.TimeSpace,
                Script = $"timespace-{timeSpaceId}",
            });
        }
    }
}
