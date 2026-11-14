//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.GameObject.Ecs;
using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class KillQuestHandlerBase(ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage) : IQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

    protected virtual bool Counts(PlayerComponentBundle character, NpcMonsterDto mob, QuestObjectiveDto objective) =>
        objective.FirstData == mob.NpcMonsterVNum;

    public async Task OnMonsterKilledAsync(PlayerComponentBundle character, NpcMonsterDto mob, CharacterQuest quest)
    {
        var progressSnapshots = new List<(short MobVNum, int Current, int Required)>();
        foreach (var objective in quest.Quest.QuestObjectives)
        {
            if (!Counts(character, mob, objective))
            {
                continue;
            }
            var required = objective.SecondData ?? 0;
            var previous = quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var p) ? p : 0;
            if (required > 0 && previous >= required)
            {
                continue;
            }
            var current = quest.ObjectiveProgress.AddOrUpdate(objective.QuestObjectiveId, 1, (_, e) => e + 1);
            if (required > 0 && current > required)
            {
                current = required;
                quest.ObjectiveProgress[objective.QuestObjectiveId] = required;
            }
            progressSnapshots.Add((mob.NpcMonsterVNum, current, required));
        }

        if (progressSnapshots.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var (mobVNum, current, required) in progressSnapshots)
            {
                await character.SendPacketAsync(character.GenerateSayi2(SayColorType.Red, Game18NConstString.Hunting, 6, mobVNum, $"{current}/{required}"));
            }
            await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, logLanguage[LogLanguageKey.QUEST_PROGRESS_SEND_FAILED],
                character.CharacterId, quest.QuestId);
        }
    }
}
