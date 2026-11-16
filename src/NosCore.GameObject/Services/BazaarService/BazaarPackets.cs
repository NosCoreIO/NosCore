//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.GameObject.Ecs;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.BazaarService;

public static class BazaarPackets
{
    public static WopenPacket GenerateBazaarWindow(this PlayerComponentBundle player, IClock clock)
    {
        var medal = player.BazaarMedal;
        return new WopenPacket
        {
            Type = WindowType.NosBazaar,
            Argument = medal == null ? (byte)0
                : medal.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte)MedalType.Gold : (byte)MedalType.Silver,
            Argument2 = medal == null ? (byte)0
                : (byte)(int)(medal.DateEnd == null ? 720 : (medal.DateEnd.Value - clock.GetCurrentInstant()).TotalHours),
        };
    }
}
