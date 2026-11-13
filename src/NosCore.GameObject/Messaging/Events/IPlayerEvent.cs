//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Messaging.Events
{
    // An event about one player. Handled after that player has left the world, it is dropped.
    public interface IPlayerEvent
    {
        bool PlayerHasLeft { get; }
    }
}
