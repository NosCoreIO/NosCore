//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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
}
