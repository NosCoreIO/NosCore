//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.GameObject.Ecs;
using NosCore.Packets.ServerPackets.Specialists;

namespace NosCore.GameObject.Services.SpecialistCardService;

public static class SpecialistPackets
{
    public static SpPacket GenerateSpPoint(this PlayerComponentBundle player, IOptions<WorldConfiguration> worldConfiguration)
    {
        return new SpPacket
        {
            AdditionalPoint = player.SpAdditionPoint,
            MaxAdditionalPoint = worldConfiguration.Value.MaxAdditionalSpPoints,
            SpPoint = player.SpPoint,
            MaxSpPoint = worldConfiguration.Value.MaxSpPoints
        };
    }
}
