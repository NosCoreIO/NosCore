//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Networking.ClientSession;
using System.Threading.Tasks;

namespace NosCore.GameObject.Infastructure
{
    public interface IEventHandler<in T, T2> : IEventHandler
    {
        bool Condition(T condition);

        Task ExecuteAsync(RequestData<T2> requestData);
    }

    public interface IEventHandler
    {
    }
}
