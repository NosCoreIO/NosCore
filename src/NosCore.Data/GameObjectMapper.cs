//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using System;
using System.Linq;

namespace NosCore.Data
{
    public class GameObjectMapper<TDto, TGameObject> : IGameObjectMapper<TDto>
    {
        public GameObjectMapper(Func<TGameObject> resolve)
        {
            TypeAdapterConfig<TDto, TGameObject>.NewConfig().ConstructUsing(_ => resolve());
        }
    }

    public static class GameObjectMapper
    {
        // Mapster cannot build a type with required members through a factory, so only types
        // that need injected constructor arguments are built by the container.
        public static bool BuildsThroughContainer(Type gameObject) =>
            gameObject.GetConstructors().Any(c => c.GetParameters().Length > 0);
    }

    public interface IGameObjectMapper<TDto>
    {
    }
}
