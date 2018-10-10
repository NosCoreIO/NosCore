using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.ServerPackets;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class GroupExtension
    {
        public static PidxPacket GeneratePidx(this Group group, INamedEntity entity)
        {
            return new PidxPacket
            {
                GroupId = group?.GroupId ?? -1,
                SubPackets = group == null ? new List<PidxSubPacket> { entity.GenerateSubPidx(false) } : group.Values.Select(s => s.Item2.GenerateSubPidx(true)).ToList()
            };
        }
    }
}
