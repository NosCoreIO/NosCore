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
        // Null when no script by that name exists, so the content it would run is simply left out.
        Task<IInstanceScript?> LoadAsync(string name);
    }
}
