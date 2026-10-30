//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // The Wing of Return saves where it was used and sends the character to its place of revival;
    // the Amulet of Return brings it back to that saved point.
    [UsedImplicitly]
    public sealed class ReturnItemHandler(IRespawnService respawnService, IMapChangeService mapChangeService)
    {
        private const byte ConfirmedMode = 1;
        private const byte DelayedMode = 2;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.Teleport
                || (item.EffectValue != (short)TeleportEffectValue.WingOfReturn
                    && item.EffectValue != (short)TeleportEffectValue.AmuletOfReturn))
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var itemInstance = evt.InventoryItem;
            var mode = evt.Packet.Mode;

            if (character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                await SayAsync(session, Game18NConstString.CanNotBeUsedHere);
                return;
            }

            if (character.IsVehicled)
            {
                await SayAsync(session, Game18NConstString.OnlyPotionInVehicle);
                return;
            }

            var mapId = character.MapInstance.Map.MapId;
            var returnPoint = respawnService.GetReturnPoint(character, mapId);
            var isWing = item.EffectValue == (short)TeleportEffectValue.WingOfReturn;

            if (!isWing && returnPoint == null)
            {
                await SayAsync(session, Game18NConstString.NoReturnPoint);
                return;
            }

            if (mode < DelayedMode)
            {
                if (isWing && returnPoint != null && mode != ConfirmedMode)
                {
                    await session.SendPacketAsync(new QnaiPacket
                    {
                        YesPacket = character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, ConfirmedMode, 0),
                        Question = Game18NConstString.OverwriteReturnPoint,
                    });
                    return;
                }

                await session.SendPacketAsync(new DelayPacket
                {
                    Delay = 5000,
                    Type = DelayPacketType.ItemInUse,
                    Packet = character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, DelayedMode, 0)
                });
                return;
            }

            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));

            if (isWing)
            {
                respawnService.SetReturnPoint(character, mapId, character.PositionX, character.PositionY);
                var (respawnMapId, x, y) = respawnService.GetRespawnLocation(character, respawnService.ResolveRespawnMapTypeId(mapId));
                await mapChangeService.ChangeMapAsync(session, respawnMapId, x, y);
                return;
            }

            await mapChangeService.ChangeMapAsync(session, returnPoint!.Value.MapId, returnPoint.Value.X, returnPoint.Value.Y);
        }

        private static Task SayAsync(ClientSession session, Game18NConstString message)
        {
            return session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Yellow,
                Message = message
            });
        }
    }
}
