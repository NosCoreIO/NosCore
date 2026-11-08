//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Npcs;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun;

public interface INrunEventHandler
{
    NrunRunnerType Runner { get; }

    // A choice offered in an NPC's dialog: only taken from an NPC on the player's map whose dialog
    // the player opened and who still stands within reach.
    bool RequiresNpc { get; }

    Task HandleAsync(ClientSession session, IAliveEntity? target, NrunPacket packet);
}
