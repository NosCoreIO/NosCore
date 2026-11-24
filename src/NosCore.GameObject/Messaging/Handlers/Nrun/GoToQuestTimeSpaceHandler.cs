//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // n_run 1000 {timeSpaceId}: the dialog button that sends the player into a quest time-space
    // that has no stone of its own.
    [UsedImplicitly]
    public sealed class GoToQuestTimeSpaceHandler(ITimeSpaceEntryService timeSpaceEntryService,
        INpcTalkButtonCatalog npcTalkButtons) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.GoToXTimeSpace;

        public bool RequiresNpc => true;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet)
        {
            if (packet.Type is not { } timeSpaceId || target is not NpcComponentBundle npc
                || !npcTalkButtons.Offers(npc, Runner, timeSpaceId))
            {
                return Task.CompletedTask;
            }

            var character = session.Character;
            return timeSpaceEntryService.EnterAsync(session, new ScriptedInstance
            {
                MapId = character.MapId,
                PositionX = character.PositionX,
                PositionY = character.PositionY,
                Type = ScriptedInstanceType.TimeSpace,
                TimeSpaceId = timeSpaceId,
                Script = $"timespace-{timeSpaceId:00}"
            });
        }
    }
}
