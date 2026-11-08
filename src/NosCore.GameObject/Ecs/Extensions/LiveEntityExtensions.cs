//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Ecs.Extensions
{
    // A player's entity is replaced on every map change, so a handle taken before one - by an
    // event waiting in a queue, say - points at an entity that is gone. These return the
    // entity the player has now, or what they were given when there is nothing newer.
    public static class LiveEntityExtensions
    {
        public static PlayerComponentBundle Live(this PlayerComponentBundle player)
        {
            if (player.World == null || player.World.IsAlive(player.Entity))
            {
                return player;
            }

            return player.World.ForwardedTo(player.Entity) is { } session && session.TryGetCharacter(out var live)
                ? live
                : player;
        }

        public static IAliveEntity Live(this IAliveEntity entity) =>
            entity is PlayerComponentBundle player ? player.Live() : entity;

        public static ICharacterEntity Live(this ICharacterEntity character) =>
            character is PlayerComponentBundle player ? player.Live() : character;
    }
}
