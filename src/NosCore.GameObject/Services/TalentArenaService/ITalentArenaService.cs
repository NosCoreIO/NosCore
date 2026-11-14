//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.GameObject.Services.TalentArenaService
{
    public interface ITalentArenaService
    {
        bool IsOpen { get; }

        // False when the Arena of Talents is already open on this channel.
        bool TryStart();

        Task RegisterAsync(ClientSession session);

        // A player who already has teammates is asked to confirm first.
        Task CancelAsync(ClientSession session, bool confirmed);
    }
}
