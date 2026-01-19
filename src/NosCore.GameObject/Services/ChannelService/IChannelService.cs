//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ChannelService
{
    public interface IChannelService
    {
        Task MoveChannelAsync(Networking.ClientSession.ClientSession clientSession, int channelId);
    }
}
