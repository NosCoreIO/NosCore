//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.PacketHandlers.Inventory
{
    public class GetPacketHandler(ILogger<GetPacketHandler> logger, IHeuristic distanceCalculator, IGroupLootService groupLootService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IMessageBus messageBus)
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
                    canpick = distanceCalculator.GetDistance(
                        (clientSession.Character.PositionX, clientSession.Character.PositionY),
                        (mapItem.PositionX, mapItem.PositionY)) < 8;
                    break;

                case VisualType.Npc:
                    return;

                default:
                    logger.LogError(logLanguage[LogLanguageKey.UNKNOWN_PICKERTYPE]);
                    return;
            }

            if (!canpick)
            {
                return;
            }

            if (!groupLootService.CanPickUp(clientSession.Character, mapItem))
            {
                await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.UnableToPickUp));
                return;
            }

            await messageBus.InvokeAsync(new MapItemPickedUpEvent(
                clientSession,
                mapItem.VisualId,
                mapItem.VNum,
                mapItem.Amount,
                mapItem.ItemInstance,
                getPacket));
        }
    }
}
