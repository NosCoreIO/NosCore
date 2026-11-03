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
using NosCore.Packets.ServerPackets.Event;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    // The Wing of Return asks whether to save where it was used, then sends the character to its place
    // of revival either way; the Amulet of Return marks the saved point and brings it back there.
    [UsedImplicitly]
    public sealed class ReturnItemHandler(IRespawnService respawnService, IMapChangeService mapChangeService)
    {
        private const byte SaveAnswered = 1;
        private const byte DontSaveAnswered = 2;
        private const byte SaveAndLeave = 3;
        private const byte LeaveWithoutSaving = 4;
        private const byte AmuletConfirmed = 1;
        private const byte AmuletLeave = 2;

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

            if (item.EffectValue == (short)TeleportEffectValue.WingOfReturn)
            {
                await UseWingAsync(evt);
                return;
            }

            await UseAmuletAsync(evt);
        }

        private async Task UseWingAsync(ItemUsedEvent evt)
        {
            var session = evt.ClientSession;
            var character = session.Character;
            var itemInstance = evt.InventoryItem;
            var pocket = (PocketType)itemInstance.Type;
            var mapId = character.MapInstance.Map.MapId;

            switch (evt.Packet.Mode)
            {
                case 0:
                    await session.SendPacketAsync(new DlgiPacket
                    {
                        YesPacket = character.GenerateUseItem(pocket, itemInstance.Slot, SaveAnswered, 0),
                        NoPacket = character.GenerateUseItem(pocket, itemInstance.Slot, DontSaveAnswered, 0),
                        Question = Game18NConstString.AmuletMessageToSavePositionAndReturnToVillage,
                    });
                    return;
                case SaveAnswered:
                case DontSaveAnswered:
                    await SendDelayAsync(evt, evt.Packet.Mode == SaveAnswered ? SaveAndLeave : LeaveWithoutSaving);
                    return;
                case SaveAndLeave:
                case LeaveWithoutSaving:
                    await ConsumeAsync(evt);
                    if (evt.Packet.Mode == SaveAndLeave)
                    {
                        respawnService.SetReturnPoint(character, mapId, character.PositionX, character.PositionY);
                    }

                    var (respawnMapId, x, y) = respawnService.GetRespawnLocation(character, respawnService.ResolveRespawnMapTypeId(mapId));
                    await mapChangeService.ChangeMapAsync(session, respawnMapId, x, y);
                    return;
            }
        }

        private async Task UseAmuletAsync(ItemUsedEvent evt)
        {
            var session = evt.ClientSession;
            var returnPoint = respawnService.GetReturnPoint(session.Character, session.Character.MapInstance.Map.MapId);
            if (returnPoint == null)
            {
                await SayAsync(session, Game18NConstString.NoReturnPoint);
                return;
            }

            var (mapId, x, y) = returnPoint.Value;
            switch (evt.Packet.Mode)
            {
                case 0:
                    var itemInstance = evt.InventoryItem;
                    await session.SendPacketAsync(new RpPacket
                    {
                        MapId = mapId,
                        X = x,
                        Y = y,
                        Packet = session.Character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, AmuletConfirmed, 0),
                    });
                    return;
                case AmuletConfirmed:
                    await SendDelayAsync(evt, AmuletLeave);
                    return;
                case AmuletLeave:
                    await ConsumeAsync(evt);
                    await mapChangeService.ChangeMapAsync(session, mapId, x, y);
                    return;
            }
        }

        private static Task SendDelayAsync(ItemUsedEvent evt, byte nextMode)
        {
            var itemInstance = evt.InventoryItem;
            return evt.ClientSession.SendPacketAsync(new DelayPacket
            {
                Delay = 5000,
                Type = DelayPacketType.ItemInUse,
                Packet = evt.ClientSession.Character.GenerateUseItem((PocketType)itemInstance.Type, itemInstance.Slot, nextMode, 0)
            });
        }

        private static Task ConsumeAsync(ItemUsedEvent evt)
        {
            var itemInstance = evt.InventoryItem;
            evt.ClientSession.Character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            return evt.ClientSession.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
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
