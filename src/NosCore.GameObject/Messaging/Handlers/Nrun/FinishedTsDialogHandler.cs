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
using NosCore.Packets.ClientPackets.Quest;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    internal static class TsDialogAdvance
    {
        public static Task AdvanceAsync(IQuestService questService, ClientSession session)
        {
            var script = session.Character.Script;
            if (script == null)
            {
                return Task.CompletedTask;
            }
            return questService.RunScriptAsync(session.Character, new ScriptClientPacket
            {
                Type = QuestActionType.Dialog,
                FirstArgument = script.ScriptId,
                SecondArgument = script.ScriptStepId,
            });
        }
    }

    [UsedImplicitly]
    public sealed class FinishedTsDialogHandler(IQuestService questService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTSDialog;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogAdvance.AdvanceAsync(questService, session);
    }

    [UsedImplicitly]
    public sealed class FinishedTsDialog2Handler(IQuestService questService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTSDialog2;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogAdvance.AdvanceAsync(questService, session);
    }

    [UsedImplicitly]
    public sealed class FinishedTsHandler(IQuestService questService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTs;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogAdvance.AdvanceAsync(questService, session);
    }

    [UsedImplicitly]
    public sealed class FinishedTs2Handler(IQuestService questService) : INrunEventHandler
    {
        public NrunRunnerType Runner => NrunRunnerType.FinishedTS2;

        public Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet) =>
            TsDialogAdvance.AdvanceAsync(questService, session);
    }
}
