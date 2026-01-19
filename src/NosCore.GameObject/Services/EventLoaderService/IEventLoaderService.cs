//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.EventLoaderService
{
    public interface IEventLoaderService<in T1, T2>
    {
        void LoadHandlers(T1 item);
    }
}
