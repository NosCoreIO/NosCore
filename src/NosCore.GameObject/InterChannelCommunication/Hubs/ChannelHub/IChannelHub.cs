//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;

public interface IChannelHub
{
    Task Bind(Channel data);

    Task<List<ChannelInfo>> GetCommunicationChannels();
    Task SetMaintenance(bool isGlobal, bool value);

    Task<bool> Ping();
}
