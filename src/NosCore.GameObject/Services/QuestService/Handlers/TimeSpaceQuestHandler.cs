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
using NosCore.GameObject.Ecs.Extensions;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// FirstData: the client's time-space id; SecondData: how many wins are needed.
[UsedImplicitly]
public sealed class TimeSpaceQuestHandler : IQuestTypeHandler
{
    public QuestType QuestType => QuestType.TimesSpace;

    public async Task<bool> OnScriptedInstanceSucceededAsync(PlayerComponentBundle character, ScriptedInstanceType type, short instanceId,
        CharacterQuest quest)
    {
        if (type != ScriptedInstanceType.TimeSpace)
        {
            return false;
        }

        return await NpcObjectiveProgress.RecordAsync(character, quest, instanceId, o => o.SecondData ?? 1,
            objective => character.SendPacketAsync(character.GenerateSayi2(SayColorType.Red, Game18NConstString.TimeSpaceStoneCompleted,
                ProgressArgument, $"{quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId)}/{objective.SecondData ?? 1}")));
    }

    private const byte ProgressArgument = 9;
}
