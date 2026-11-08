//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.MateService
{
    public interface IMateControlService
    {
        Task<bool> MoveAsync(PlayerComponentBundle owner, long mateTransportId, short x, short y);

        Task<bool> TalkAsync(PlayerComponentBundle owner, long mateTransportId, string? message);
    }
}
