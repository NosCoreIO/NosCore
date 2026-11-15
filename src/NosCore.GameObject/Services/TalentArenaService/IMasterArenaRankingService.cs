//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Services.TalentArenaService
{
    public interface IMasterArenaRankingService
    {
        TARankPacket GetRanking();
    }
}
