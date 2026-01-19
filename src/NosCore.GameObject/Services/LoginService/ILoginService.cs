//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Packets.ClientPackets.Login;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.LoginService
{
    public interface ILoginService
    {
        Task LoginAsync(string? username, string md5String, ClientVersionSubPacket clientVersion,
            Networking.ClientSession.ClientSession clientSession, string passwordToken, bool useApiAuth, RegionType language);
    }
}
