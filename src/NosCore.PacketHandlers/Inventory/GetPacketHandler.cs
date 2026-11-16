//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs.Extensions;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.PathFinder.Interfaces;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class GetPacketHandler(ILogger<GetPacketHandler> logger, IHeuristic distanceCalculator, IGroupLootService groupLootService,
            ILogLanguageLocalizer<LogLanguageKey> logLanguage, IGameEventDispatcher gameEvents, IMapButtonService mapButtonService)
        : PacketHandler<GetPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(GetPacket getPacket, ClientSession clientSession)
        {
            var mapInstance = clientSession.Character.MapInstance;
            var mapItemNullable = mapInstance.TryGetMapItem(getPacket.VisualId);
            if (mapItemNullable == null)
            {
                if (getPacket.PickerType == VisualType.Player)
                {
                    await mapButtonService.PressAsync(clientSession.Character, getPacket.VisualId);
                }

                return;
            }

            var mapItem = mapItemNullable.Value;

            var character = clientSession.Character;
            (short X, short Y) pickerPosition;
            switch (getPacket.PickerType)
            {
                case VisualType.Player:
                    pickerPosition = (character.PositionX, character.PositionY);
                    break;

                case VisualType.Npc when character.Mates.TryGetValue(getPacket.PickerId, out var mate)
                    && mate.CanPickUp && mate.Entity is { } pet:
                    pickerPosition = (pet.PositionX, pet.PositionY);
                    break;

                case VisualType.Npc:
                    return;

                default:
                    logger.LogError(logLanguage[LogLanguageKey.UNKNOWN_PICKERTYPE]);
                    return;
            }

            var canpick = distanceCalculator.GetDistance(pickerPosition, (mapItem.PositionX, mapItem.PositionY)) < 8;
            if (!canpick)
            {
                return;
            }

            if (!groupLootService.CanPickUp(clientSession.Character, mapItem))
            {
                await clientSession.SendPacketAsync(clientSession.Character.GenerateSayi(SayColorType.Yellow, Game18NConstString.UnableToPickUp));
                return;
            }

            await gameEvents.RaiseAsync(new MapItemPickedUpEvent(
                clientSession,
                mapItem.VisualId,
                mapItem.VNum,
                mapItem.Amount,
                mapItem.ItemInstance,
                getPacket));
        }
    }
}
