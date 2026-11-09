//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Packets.ClientPackets.Quicklist;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using System;
using System.Collections.Generic;

namespace NosCore.GameObject.Ecs.Extensions;

// Each specialist form keeps its own quicklist, keyed by the morph worn when the entry was set.
public static class QuicklistExtensions
{
    public static QsetClientPacket SetQuicklistEntry(this PlayerComponentBundle player, QSetType type,
        short quickListIndex, short slot, short iconType, short iconVNum)
    {
        var morph = player.QuicklistMorph();
        player.QuicklistEntries.RemoveAll(
            n => (n.QuickListIndex == quickListIndex) && (n.Slot == slot) && (n.Morph == morph));
        player.QuicklistEntries.Add(new QuicklistEntryDto
        {
            Id = Guid.NewGuid(),
            CharacterId = player.CharacterId,
            Type = (short)type,
            QuickListIndex = quickListIndex,
            Slot = slot,
            IconType = iconType,
            IconVNum = iconVNum,
            Morph = morph
        });
        return GenerateQset(quickListIndex, slot, type, iconType, iconVNum);
    }

    // Swaps with whatever occupies the destination, or empties the origin when nothing does.
    public static IReadOnlyList<QsetClientPacket> MoveQuicklistEntry(this PlayerComponentBundle player,
        short fromIndex, short fromSlot, short toIndex, short toSlot)
    {
        var morph = player.QuicklistMorph();
        var qlFrom = player.QuicklistEntries.Find(n =>
            (n.QuickListIndex == fromIndex) && (n.Slot == fromSlot) && (n.Morph == morph));
        if (qlFrom == null)
        {
            return [];
        }

        var qlTo = player.QuicklistEntries.Find(n =>
            (n.QuickListIndex == toIndex) && (n.Slot == toSlot) && (n.Morph == morph));

        qlFrom.QuickListIndex = toIndex;
        qlFrom.Slot = toSlot;
        var moved = GenerateQset(qlFrom);

        if (qlTo == null)
        {
            return [moved, GenerateReset(fromIndex, fromSlot)];
        }

        qlTo.QuickListIndex = fromIndex;
        qlTo.Slot = fromSlot;
        return [moved, GenerateQset(qlTo)];
    }

    public static QsetClientPacket RemoveQuicklistEntry(this PlayerComponentBundle player, short quickListIndex,
        short slot)
    {
        var morph = player.QuicklistMorph();
        player.QuicklistEntries.RemoveAll(
            n => (n.QuickListIndex == quickListIndex) && (n.Slot == slot) && (n.Morph == morph));
        return GenerateReset(quickListIndex, slot);
    }

    private static short QuicklistMorph(this PlayerComponentBundle player) => player.UseSp ? player.Morph : (short)0;

    private static QsetClientPacket GenerateQset(QuicklistEntryDto entry) =>
        GenerateQset(entry.QuickListIndex, entry.Slot, (QSetType)entry.Type, entry.IconType, entry.IconVNum);

    private static QsetClientPacket GenerateReset(short quickListIndex, short slot) =>
        GenerateQset(quickListIndex, slot, QSetType.Reset, 7, -1);

    private static QsetClientPacket GenerateQset(short quickListIndex, short slot, QSetType type, short data1,
        short data2) => new()
    {
        OriginQuickList = quickListIndex,
        OriginQuickListSlot = slot,
        Data = new QsetClientSubPacket
        {
            Type = type,
            OriginQuickList = data1,
            OriginQuickListSlot = data2,
            Data = 0
        }
    };
}
