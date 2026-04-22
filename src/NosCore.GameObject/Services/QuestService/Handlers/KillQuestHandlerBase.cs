//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.Packets;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using Serilog;

namespace NosCore.GameObject.Services.QuestService.Handlers;

public abstract class KillQuestHandlerBase(ILogger logger) : IQuestTypeHandler
{
    public abstract QuestType QuestType { get; }

    public async Task OnMonsterKilledAsync(ICharacterEntity character, NpcMonsterDto mob, CharacterQuest quest)
    {
        var progressed = false;
        short mobVNum = 0;
        int current = 0;
        int required = 0;
        foreach (var objective in quest.Quest.QuestObjectives)
        {
            if (objective.FirstData != mob.NpcMonsterVNum)
            {
                continue;
            }
            required = objective.SecondData ?? 0;
            var previous = quest.ObjectiveProgress.TryGetValue(objective.QuestObjectiveId, out var p) ? p : 0;
            if (required > 0 && previous >= required)
            {
                continue;
            }
            current = quest.ObjectiveProgress.AddOrUpdate(objective.QuestObjectiveId, 1, (_, e) => e + 1);
            if (required > 0 && current > required)
            {
                current = required;
                quest.ObjectiveProgress[objective.QuestObjectiveId] = required;
            }
            mobVNum = (short)objective.FirstData;
            progressed = true;
        }

        if (!progressed)
        {
            return;
        }

        try
        {
            await character.SendPacketAsync(new Sayi2Packet
            {
                VisualType = VisualType.Player,
                VisualId = character.VisualId,
                Type = SayColorType.Red,
                Message = Game18NConstString.Hunting,
                ArgumentType = 6,
                Game18NArguments = new Game18NArguments(2) { mobVNum, $"{current}/{required}" }
            });
            await character.SendPacketAsync(quest.GenerateQstiPacket(false));
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to send quest progress for character {CharacterId} quest {QuestId}",
                character.CharacterId, quest.QuestId);
        }
    }
}
