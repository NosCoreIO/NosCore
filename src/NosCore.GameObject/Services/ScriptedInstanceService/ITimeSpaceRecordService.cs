//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ServerPackets.Event;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ScriptedInstanceService
{
    public interface ITimeSpaceRecordService
    {
        Task<RbrSubPacketHighScore> GetHighScoreAsync(short timeSpaceId);

        // True when the score beats the time-space's best, which it then replaces.
        Task<bool> TryBreakAsync(short timeSpaceId, long characterId, int score);
    }
}
