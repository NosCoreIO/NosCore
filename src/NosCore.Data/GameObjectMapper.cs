//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Mapster;
using System;

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
