//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.UI;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.MateService;

public static class MatePackets
{
    public static IEnumerable<IPacket> GenerateMateRoster(this PlayerComponentBundle player,
        IPartnerSkillCatalog partnerSkills) =>
        new IPacket[] { new PclearPacket() }
            .Concat(MateService.GenerateScPackets(player.Mates.Values, player.AccountLanguage, player.InventoryService,
                partnerSkills));
}
