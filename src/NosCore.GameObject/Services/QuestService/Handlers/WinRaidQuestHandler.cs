//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Interaction;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// FirstData: the client's raid id; SecondData: how many wins are needed.
[UsedImplicitly]
public sealed class WinRaidQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.WinRaid;

    public Task<bool> OnScriptedInstanceSucceededAsync(PlayerComponentBundle character, ScriptedInstanceType type, byte instanceId,
        CharacterQuest quest) =>
        type == ScriptedInstanceType.Raid
            ? NpcObjectiveProgress.RecordAsync(character, quest, instanceId, o => o.SecondData ?? 1)
            : Task.FromResult(false);
}
