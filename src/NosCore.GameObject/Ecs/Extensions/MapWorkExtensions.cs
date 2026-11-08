//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;

namespace NosCore.GameObject.Ecs.Extensions
{
    public static class MapWorkExtensions
    {
        // HP, MP and experience change on the entity's map executor, so a hit, a regeneration
        // tick, a potion and a reward never read and write over one another.
        public static Task OnMapAsync(this IAliveEntity entity, Func<Task> work)
        {
            var map = entity.MapInstance;
            return map == null ? work() : map.Executor.RunAsync(work);
        }

        public static Task<T> OnMapAsync<T>(this IAliveEntity entity, Func<Task<T>> work)
        {
            var map = entity.MapInstance;
            return map == null ? work() : map.Executor.RunAsync(work);
        }
    }
}
