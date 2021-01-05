using System.Collections.Generic;
using System.Threading.Tasks;
using NosCore.Data.WebApi;

namespace NosCore.Core.HubInterfaces
{
    public interface IChannelHub
    {
        Task Subscribe(Channel data);
        Task<List<ChannelInfo>> GetChannels();
        Task RegisterAccount(ConnectedAccount account);
        Task UnregisterAccount(string accountName);
    }
}
