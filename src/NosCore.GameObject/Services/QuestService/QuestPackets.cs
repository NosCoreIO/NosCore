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
    // The client ignores a qnpc line with fewer groups than this.
    private const int QnpcGroups = 10;

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

    // A group is the NPC's vnum, its map and the chapter's level, which the client's "search for new
    // quests" button reads back as a chat line. A taken chapter keeps its map and level under vnum -1.
    public static QnpcPacket GenerateQnpcPacket(this ScriptOfferDto offer, bool taken) => new()
    {
        SubPackets =
        [
            new QnpcSubPacket { Unknown0 = taken ? (short)-1 : offer.NpcVNum, Unknown1 = offer.MapId, Unknown2 = offer.LevelMinimum },
            .. Enumerable.Range(1, QnpcGroups - 1).Select(_ => new QnpcSubPacket()),
        ],
    };
}
