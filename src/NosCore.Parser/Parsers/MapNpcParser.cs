using System.Collections.Generic;
using NosCore.Data.AliveEntities;

namespace NosCore.Parser.Parsers
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
