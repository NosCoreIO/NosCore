using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.Enumerations.Account;
using NosCore.Data.WebApi;
using NosCore.GameObject.Networking;

namespace NosCore.WorldServer.GraphQl
{
    public class Mutation
    {
        public async Task<PostedPacket> PostPacket(PostedPacket postedPacket)
        {
            return postedPacket;
        }
    }
}
