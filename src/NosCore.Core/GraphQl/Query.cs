using System.Collections.Generic;
using System.Linq;
using NosCore.Core;
using NosCore.Core.Networking;

namespace NosCore.MasterServer.GraphQl
{
    public class Query
    {
        //[AuthorizeRole(AuthorityType.GameMaster)]
        public List<ChannelInfo> GetChannels(long? id)
        {
            if (id != null)
            {
                return MasterClientListSingleton.Instance.Channels.Where(s => s.Id == id).ToList();
            }

            return MasterClientListSingleton.Instance.Channels;
        }
    }
}
