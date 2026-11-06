//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.Instances.Abstractions
{
    public interface IInstanceScriptLoader
    {
        Task<IInstanceScript> LoadAsync(string name);
    }
}
