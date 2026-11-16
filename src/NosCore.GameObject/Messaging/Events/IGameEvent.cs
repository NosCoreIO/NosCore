//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Messaging.Events
{
    // Raised and handled on the world server, in order and before the raiser goes on, through
    // IGameEventDispatcher. Work that crosses a server or waits for later goes on Wolverine as a
    // message carrying ids and values, never an entity or a session.
    public interface IGameEvent;
}
