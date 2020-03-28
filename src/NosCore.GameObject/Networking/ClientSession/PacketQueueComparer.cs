using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking.ClientSession
{
    public class PacketQueueComparer : IComparer<Tuple<ushort?, Task>>
    {
        public int Compare([AllowNull] Tuple<ushort?, Task> x, [AllowNull] Tuple<ushort?, Task> y)
        {
            Console.Write($"{x.Item1} {y.Item1}");
            if (x.Item1 > y.Item1)
                return -1;
            if (x.Item1 < y.Item1)
                return 1;
            return 0;
        }
    }
}