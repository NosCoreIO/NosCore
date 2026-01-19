//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public interface IMapInstanceEntranceEventHandler : IEventHandler<MapInstance, MapInstance>
    {
    }
}
