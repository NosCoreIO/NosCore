//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NodaTime;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class GetPacketHandler(ILogger logger, IHeuristic distanceCalculator, IClock clock,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        : PacketHandler<GetPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GetPacket getPacket, ClientSession clientSession)
        {
            var mapInstance = clientSession.Character.MapInstance;
            var mapItemNullable = mapInstance.TryGetMapItem(getPacket.VisualId);
            if (mapItemNullable == null)
            {
                return;
            }

            var mapItem = mapItemNullable.Value;

            bool canpick;
            switch (getPacket.PickerType)
            {
                case VisualType.Player:
                    canpick = distanceCalculator.GetDistance((clientSession.Character.PositionX, clientSession.Character.PositionY),
                        (mapItem.PositionX, mapItem.PositionY)) < 8;
                    break;

                case VisualType.Npc:
                    return;

                default:
                    logger.Error(logLanguage[LogLanguageKey.UNKNOWN_PICKERTYPE]);
                    return;
            }

            if (!canpick)
            {
                return;
            }

            //TODO add group drops
            if ((mapItem.OwnerId != null) && (mapItem.DroppedAt.Plus(Duration.FromSeconds(30)) > clock.GetCurrentInstant()) &&
                (mapItem.OwnerId != clientSession.Character.CharacterId))
            {
                await clientSession.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = clientSession.Character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.UnableToPickUp
                });
                return;
            }

            if (!mapInstance.MapItemRequestContexts.TryGetValue(mapItem.VisualId, out var context))
            {
                return;
            }

            context.PickupSubject.OnNext(new RequestData<Tuple<MapItemComponentBundle, GetPacket>>(clientSession,
                new Tuple<MapItemComponentBundle, GetPacket>(mapItem, getPacket)));

            await Task.WhenAll(context.HandlerTasks);
        }
    }
}
