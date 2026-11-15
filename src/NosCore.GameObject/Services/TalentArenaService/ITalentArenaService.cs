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

        bool IsMastersOpen { get; }

        // False when the Arena of Masters is already open on this channel.
        bool TryStartMasters();

        // Registers the player's party of three for the Arena of Masters.
        Task RegisterMastersAsync(ClientSession session);

        // A player who already has teammates is asked to confirm first.
        Task CancelAsync(ClientSession session, bool confirmed);

        // Takes a player from the arena into the stands of the match the named fighter is in, or of
        // any match running when no name is given.
        Task WatchAsync(ClientSession session, string? fighterName);

        // A fighter in the ring calls one of their two teammates in to help for a while.
        Task CallAsync(ClientSession session, byte calledIndex);
    }
}
