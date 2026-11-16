//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs.Extensions;

// Only one title can be shown and only one can have its effect on at a time.
public static class TitleExtensions
{
    public static bool ToggleTitleVisibility(this PlayerComponentBundle player, short titleType)
    {
        var selected = player.Titles.FirstOrDefault(s => s.TitleType == titleType);
        if (selected == null)
        {
            return false;
        }

        foreach (var title in player.Titles.Where(s => s.TitleType != titleType))
        {
            title.Visible = false;
        }

        selected.Visible = !selected.Visible;
        return true;
    }

    public static bool ToggleTitleEffect(this PlayerComponentBundle player, short titleType)
    {
        var selected = player.Titles.FirstOrDefault(s => s.TitleType == titleType);
        if (selected == null)
        {
            return false;
        }

        foreach (var title in player.Titles.Where(s => s.TitleType != titleType))
        {
            title.Active = false;
        }

        selected.Active = !selected.Active;
        return true;
    }

    public static TitleInfoPacket GenerateTitInfo(this PlayerComponentBundle player)
    {
        var visibleTitle = player.Titles.FirstOrDefault(s => s.Visible)?.TitleType;
        var effectiveTitle = player.Titles.FirstOrDefault(s => s.Active)?.TitleType;
        return new TitleInfoPacket
        {
            VisualId = player.VisualId,
            EffectiveTitle = effectiveTitle ?? 0,
            VisualType = VisualType.Player,
            VisibleTitle = visibleTitle ?? 0
        };
    }

    public static TitlePacket GenerateTitle(this PlayerComponentBundle player)
    {
        var data = player.Titles.Select(s => new TitleSubPacket
        {
            TitleId = (short)(s.TitleType - 9300),
            TitleStatus = (byte)((s.Visible ? 2 : 0) + (s.Active ? 4 : 0) + 1)
        }).ToList() as List<TitleSubPacket?>;
        return new TitlePacket
        {
            Data = data.Any() ? data : null
        };
    }

    public static TitPacket GenerateTit(this PlayerComponentBundle player)
    {
        return new TitPacket
        {
            ClassType = (Game18NConstString)Enum.Parse(typeof(Game18NConstString), player.Class.ToString()),
            Name = player.Name
        };
    }
}
