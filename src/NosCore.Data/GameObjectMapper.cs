using System;
using Mapster;

namespace NosCore.Data
{
    public class GameObjectMapper<TDto, TGameObject> : IGameObjectMapper<TDto>
    {
        public GameObjectMapper(Func<TGameObject> resolve)
        {
            TypeAdapterConfig<TDto, TGameObject>.NewConfig().ConstructUsing(_ => resolve());
        }
    }

    public interface IGameObjectMapper<TDto>
    {
    }
}
