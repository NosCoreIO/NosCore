using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
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
