//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.QuestService.Handlers;

// Progress for objectives whose FirstData names an NPC (or a time-space). Apart from
// time-spaces these quest types are not counted in qsti, so the tally lives in
// ObjectiveProgress only to know when every target has been reached.
internal static class NpcObjectiveProgress
{
    public static bool Advance(CharacterQuest quest, short npcVNum, Func<QuestObjectiveDto, int> target) =>
        Advance(quest, npcVNum, target, []);

    private static bool Advance(CharacterQuest quest, short npcVNum, Func<QuestObjectiveDto, int> target,
        List<QuestObjectiveDto> advancedObjectives)
    {
        var advanced = false;
        foreach (var objective in quest.Quest.QuestObjectives.Where(o => o.FirstData == npcVNum))
        {
            var required = target(objective);
            var current = quest.ObjectiveProgress.GetValueOrDefault(objective.QuestObjectiveId);
            if (current >= required)
            {
                continue;
            }

            quest.ObjectiveProgress[objective.QuestObjectiveId] = current + 1;
            advancedObjectives.Add(objective);
            advanced = true;
        }

        return advanced;
    }

    public static bool AllReached(CharacterQuest quest, Func<QuestObjectiveDto, int> target) =>
        quest.Quest.QuestObjectives.All(o => quest.ObjectiveProgress.GetValueOrDefault(o.QuestObjectiveId) >= target(o));

    // Counts one more of the NPC towards the quest: true once every objective is reached,
    // otherwise the client's quest window is refreshed when anything moved.
    public static async Task<bool> RecordAsync(PlayerComponentBundle character, CharacterQuest quest, short npcVNum,
        Func<QuestObjectiveDto, int> target, Func<QuestObjectiveDto, Task>? onAdvanced = null)
    {
        var advancedObjectives = new List<QuestObjectiveDto>();
        if (!Advance(quest, npcVNum, target, advancedObjectives))
        {
            return false;
        }

        foreach (var objective in advancedObjectives)
        {
            if (onAdvanced != null)
            {
                await onAdvanced(objective);
            }
        }

        if (AllReached(quest, target))
        {
            return true;
        }

        await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        return false;
    }

    // "Talked to <NPC>.", said when an NPC takes the items or gold a quest brings it.
    public static Sayi2Packet TalkedToLine(this PlayerComponentBundle character, short npcVNum) =>
        character.GenerateSayi2(SayColorType.Red, Game18NConstString.TalkedTo, NpcArgument, npcVNum);

    private const byte NpcArgument = 6;

    public static int Times(QuestObjectiveDto objective) => objective.ThirdData is > 0 and var times ? times : 1;
}
