using OpenNosCore.Core.Logger;
using OpenNosCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNosCore.Parser
{
    public class MapNpcParser
    {

        private static readonly List<MapNpcDTO> Npcs = new List<MapNpcDTO>();
        private static readonly Dictionary<long, short> EffPacketsDictionary = new Dictionary<long, short>();
        private static readonly List<long> NpcMvPacketsList = new List<long>();

        public void InsertMapNpcs(List<string[]> packetList)
        {
          
        }
    }
}
