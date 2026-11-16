//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.Packets.ServerPackets.Quest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.QuestService;

public static class QuestPackets
{
    public static QstlistPacket GenerateQuestPacket(this PlayerComponentBundle visualEntity, CharacterQuest? taken = null)
    {
        return new QstlistPacket(visualEntity.Quests.Values
            .Where(s => s.CompletedOn == null).Select(quest => quest.GenerateQuestSubPacket(quest == taken)).ToList());
    }

    public static SqstPacket GenerateCompletedQuests(this PlayerComponentBundle visualEntity, byte block, NodaTime.Instant now) =>
        SqstPacket.FromCompletedQuests(block, visualEntity.Quests.Values
            .Where(q => q.IsDoneAt(now))
            .Select(q => (int)q.QuestId));

    public static IEnumerable<SqstPacket> GenerateCompletedQuests(this PlayerComponentBundle visualEntity, IEnumerable<QuestDto> quests,
        NodaTime.Instant now) =>
        Enumerable.Range(0, SqstPacket.BlockOf(quests.Select(q => (int)q.QuestId).DefaultIfEmpty().Max()) + 1)
            .Select(block => visualEntity.GenerateCompletedQuests((byte)block, now));
}
