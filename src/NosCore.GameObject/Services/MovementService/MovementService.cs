//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Map;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.GameObject.Services.MapInstanceAccessService;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Networking;
using NosCore.Networking.SessionGroup.ChannelMatcher;
using NosCore.Packets.Enumerations;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.Services.MovementService;

public interface IMovementService
{
    Task<WalkResult> WalkAsync(ClientSession session, short x, short y, short speed, byte checksum);

    Task<PortalResult> UsePortalAsync(ClientSession session);
}

public sealed class MovementService(IHeuristic distanceCalculator, ILogger<MovementService> logger,
        ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMessageBus messageBus, IWalkService walkService,
        IMapInstanceAccessorService mapInstanceAccessorService, IMinilandService minilandService, IClock clock,
        IMapChangeService mapChangeService, IBuffService buffService)
    : IMovementService
{
    // Leaves room for network jitter so it is not counted as a speed hack.
    private const double SpeedDiffAllowed = 1D / 3;

    // Packets of one walk arrive well within this of each other.
    private static readonly Duration SameWalk = Duration.FromMilliseconds(1500);

    public async Task<WalkResult> WalkAsync(ClientSession session, short x, short y, short speed, byte checksum)
    {
        var character = session.Character;
        if (character.NoMove || buffService.HasEffect(character, BCardEffect.MoveMovementImpossible))
        {
            return WalkResult.Refused;
        }

        var distance = (int)distanceCalculator.GetDistance((character.PositionX, character.PositionY), (x, y)) - 1;

        if (character.Speed < speed)
        {
            logger.LogWarning(
                logLanguage[LogLanguageKey.WALK_DROPPED_SPEED],
                character.Speed, speed, character.VisualId,
                character.PositionX, character.PositionY, x, y);
            return WalkResult.Refused;
        }

        if (distance > character.Speed / 2)
        {
            logger.LogWarning(
                logLanguage[LogLanguageKey.WALK_DROPPED_DISTANCE],
                distance, character.Speed / 2, character.VisualId,
                character.PositionX, character.PositionY, x, y, speed);
            return WalkResult.Refused;
        }

        if ((x + y) % 3 % 2 != checksum)
        {
            logger.LogError(logLanguage[LogLanguageKey.WALK_CHECKSUM_INVALID], character.VisualId);
            return WalkResult.Rejected;
        }

        var travelTime = 2500 / speed * distance;
        if (travelTime > 1000 * (SpeedDiffAllowed + 1))
        {
            logger.LogError(logLanguage[LogLanguageKey.SPEED_INVALID], character.VisualId);
            return WalkResult.Rejected;
        }

        var previousMove = character.LastMove;
        if (!walkService.TryStep(character, x, y))
        {
            logger.LogWarning(
                logLanguage[LogLanguageKey.WALK_DROPPED_UNREACHABLE],
                character.VisualId, x, y, character.PositionX, character.PositionY, character.Speed);
            return WalkResult.Refused;
        }

        // The position is written before `mv` is broadcast: GenerateMove and every observer
        // read it from the ECS.
        var (fromX, fromY) = clock.GetCurrentInstant() - previousMove < SameWalk
            ? (character.PreviousWalkX, character.PreviousWalkY)
            : (character.PositionX, character.PositionY);
        character.Direction = Facing.Towards(x - fromX, y - fromY, character.Direction);
        character.PreviousWalkX = character.PositionX;
        character.PreviousWalkY = character.PositionY;
        character.PositionX = x;
        character.PositionY = y;
        if (character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
        {
            character.MapX = x;
            character.MapY = y;
        }

        logger.LogInformation(
            logLanguage[LogLanguageKey.WALK_ACCEPTED],
            character.VisualId, x, y,
            character.PositionX, character.PositionY,
            character.MapX, character.MapY);

        character.CancelDelay();

        await character.MapInstance.SendPacketAsync(character.GenerateMove(),
            new EveryoneBut(session.ConnectedChannel.Id));

        await messageBus.PublishAsync(new CharacterMovedEvent(
            character, character.MapX, character.MapY, character.MapId));
        return WalkResult.Accepted;
    }

    public async Task<PortalResult> UsePortalAsync(ClientSession session)
    {
        var character = session.Character;
        if (((clock.GetCurrentInstant() - character.LastPortal).TotalSeconds < 4) ||
            (character.LastPortal > character.LastMove))
        {
            return PortalResult.TooSoon;
        }

        var portals = new List<Portal>();
        portals.AddRange(character.MapInstance.Portals);
        portals.AddRange(minilandService
            .GetMinilandPortals(character.CharacterId)
            .Where(s => s.SourceMapInstanceId == character.MapInstanceId));
        var portal = portals.Find(port =>
            distanceCalculator.GetDistance((character.PositionX, character.PositionY), (port.SourceX, port.SourceY))
            <= 2);
        if (portal == null || portal.DestinationMapInstanceId == default || !IsPortalAccessible(portal.Type))
        {
            return PortalResult.NoPortal;
        }

        character.LastPortal = clock.GetCurrentInstant();

        if ((mapInstanceAccessorService.GetMapInstance(portal.SourceMapInstanceId)?.MapInstanceType
                != MapInstanceType.BaseMapInstance)
            && (mapInstanceAccessorService.GetMapInstance(portal.DestinationMapInstanceId)?.MapInstanceType
                == MapInstanceType.BaseMapInstance))
        {
            await mapChangeService.ChangeMapAsync(session, character.MapId, character.MapX, character.MapY);
        }
        else
        {
            await mapChangeService.ChangeMapInstanceAsync(session, portal.DestinationMapInstanceId, portal.DestinationX,
                portal.DestinationY);
        }

        return PortalResult.Used;
    }

    private static bool IsPortalAccessible(PortalType type) => type switch
    {
        PortalType.MapPortal => true,
        PortalType.TsNormal => true,
        PortalType.Open => true,
        PortalType.Miniland => true,
        PortalType.TsEnd => true,
        PortalType.Exit => true,
        PortalType.Effect => true,
        PortalType.TimeSpace => true,
        PortalType.ShopTeleport => true,
        _ => false,
    };
}
