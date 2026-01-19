//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ServerPackets.Shop;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.ComponentEntities.Extensions
{
    public static class GroupExtension
    {
        public static PidxPacket GeneratePidx(this Group group, INamedEntity entity)
        {
            return new PidxPacket
            {
                GroupId = group.Count == 1 ? -1 : group.GroupId,
                SubPackets = group.Count == 1 ? new List<PidxSubPacket?> { entity.GenerateSubPidx(true) }
                    : group.Values.Select(s => s.Item2.GenerateSubPidx()).ToList() as List<PidxSubPacket?>
            };
        }
    }
}
