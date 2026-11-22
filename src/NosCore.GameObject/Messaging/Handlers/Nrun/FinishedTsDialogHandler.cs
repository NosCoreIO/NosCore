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
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    // The client closes a time-space talk with any of these runners, depending on the talk.
    internal static class TsDialogClose
    {
        public static async Task CloseAsync(IQuestService questService, IScriptedInstanceService scriptedInstanceService,
            ClientSession session)
        {
            if (session.Character.Script is { } script)
            {
                await questService.RunScriptAsync(session.Character, new ScriptClientPacket
                {
                    Type = QuestActionType.Dialog,
                    FirstArgument = script.ScriptId,
                    SecondArgument = script.ScriptStepId,
                });
            }

            if (scriptedInstanceService.GetRun(session.Character.MapInstance.MapInstanceId) is { } run)
            {
                await run.DialogClosedAsync(session.Character.CharacterId);
            }
        }
    }

    [UsedImplicitly]
    public sealed class FinishedTsDialogHandler(IQuestService questService, IScriptedInstanceService scriptedInstanceService)
        : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTSDialog;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogClose.CloseAsync(questService, scriptedInstanceService, session);
    }

    [UsedImplicitly]
    public sealed class FinishedTsDialog2Handler(IQuestService questService, IScriptedInstanceService scriptedInstanceService)
        : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTSDialog2;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogClose.CloseAsync(questService, scriptedInstanceService, session);
    }

    [UsedImplicitly]
    public sealed class FinishedTsHandler(IQuestService questService, IScriptedInstanceService scriptedInstanceService)
        : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTs;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogClose.CloseAsync(questService, scriptedInstanceService, session);
    }

    [UsedImplicitly]
    public sealed class FinishedTs2Handler(IQuestService questService, IScriptedInstanceService scriptedInstanceService)
        : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTS2;

        public bool RequiresNpc => false;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogClose.CloseAsync(questService, scriptedInstanceService, session);
    }
}
