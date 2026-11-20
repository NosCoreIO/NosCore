//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.MateService;
using NosCore.GameObject.Services.NpcDialogService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Services.MapTeleporterService;

public sealed class MapTeleporterService(List<TeleporterDto> teleporters, INpcDialogService npcDialogService, IClock clock)
    : IMapTeleporterService
{
    public const short Delay = 5000;

    public async Task<bool> StartAsync(PlayerComponentBundle character, NpcComponentBundle npc)
    {
        if (DestinationOf(npc) is not { } destination)
        {
            return false;
        }

        await character.StartDelayAsync(new DelayPacket
        {
            Delay = Delay,
            Type = DelayPacketType.Button,
            Packet = new ClientGuriPacket
            {
                Type = GuriPacketType.MapTeleporter,
                Argument = destination.MapX,
                VisualId = destination.MapY,
                Data = npc.VisualId
            }
        }, npc.VisualId, clock.GetCurrentInstant());
        return true;
    }

    public async Task<bool> CompleteAsync(PlayerComponentBundle character, int mapNpcId, short x, short y)
    {
        if (character.MapInstance.GetNpcById(mapNpcId) is not { } npc
            || DestinationOf(npc) is not { } destination
            || destination.MapX != x || destination.MapY != y
            || !npcDialogService.IsWithinReach(character, npc)
            || !character.TryCompleteDelay(DelayPacketType.Button, npc.VisualId, clock.GetCurrentInstant()))
        {
            return false;
        }

        await character.TeleportAsync(x, y);
        var mates = character.Mates.Values.Where(m => m.IsTeamMember && m.Entity is { } entity
            && entity.MapInstance == character.MapInstance);
        foreach (var (mate, mateX, mateY) in MatePlacement.Arrange(x, y, character.MapInstance.Map, mates.ToList()))
        {
            if (mate.Entity is { } entity)
            {
                await entity.TeleportAsync(mateX, mateY);
            }
        }

        return true;
    }

    private TeleporterDto? DestinationOf(NpcComponentBundle npc) =>
        teleporters.FirstOrDefault(t => t.MapNpcId == npc.VisualId && t.Type == TeleporterType.TeleporterOnMapAfterBar);
}
