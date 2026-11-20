//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BoxService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.Map;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.MapItem
{
    // A box lying on a map is opened where it lies, once the player's bar has run, and what it
    // holds goes straight into the inventory.
    [UsedImplicitly]
    public sealed class MapBoxHandler(IBoxRollService boxRoll, IItemGenerationService itemProvider,
        IMapItemGenerationService mapItemProvider, IClock clock, IGameEventDispatcher gameEvents)
    {
        public const short OpenDelay = 2000;
        private const ushort OpenedEffect = 194;
        // The opened box's ground effect carries this in place of a cell.
        private const string OpenedEffectCell = "9999";

        [UsedImplicitly]
        public async Task Handle(MapItemPickedUpEvent evt)
        {
            if (evt.ItemInstance is not { Item: { ItemType: ItemType.Map, Effect: ItemEffectType.BoxEffect } } box
                || evt.Packet.PickerType != VisualType.Player)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (evt.Packet.IsDelayed == 0)
            {
                await character.StartDelayAsync(new DelayPacket
                {
                    Delay = OpenDelay,
                    Type = DelayPacketType.OpeningBox,
                    Packet = new GetPacket
                    {
                        PickerType = VisualType.Player,
                        PickerId = character.CharacterId,
                        VisualId = evt.VisualId,
                        IsDelayed = 1
                    }
                }, evt.VisualId, clock.GetCurrentInstant());
                return;
            }

            var mapInstance = character.MapInstance;
            if (mapInstance.TryGetMapItem(evt.VisualId) is not { } lying
                || !character.TryCompleteDelay(DelayPacketType.OpeningBox, evt.VisualId, clock.GetCurrentInstant()))
            {
                return;
            }

            var (boxes, spot) = (lying.Amount, (lying.PositionX, lying.PositionY, lying.OwnerId));
            var opened = new GroundEffectPacket
            {
                Effect = OpenedEffect,
                XYCoordinates = OpenedEffectCell,
                MapX = (ushort)lying.PositionX,
                MapY = (ushort)lying.PositionY
            };
            var contents = boxRoll.Roll(box, character);
            if (contents == null)
            {
                if (!mapInstance.TryRemoveMapItem(evt.VisualId))
                {
                    return;
                }

                var leftOver = LeaveTheRest(mapInstance, boxes, box, spot);
                await mapInstance.SendPacketAsync(opened);
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.BoxIsEmpty });
                await mapInstance.SendPacketAsync(evt.GenerateGet());
                await ShowAsync(mapInstance, leftOver);
                return;
            }

            var rewards = contents.Select(r => itemProvider.Create(r.ItemGeneratedVNum, r.ItemGeneratedAmount,
                r.IsRareRandom ? (sbyte)box.Rare : (sbyte)0, r.ItemGeneratedUpgrade)).ToList();
            var inventory = character.InventoryService;
            if (!inventory.HasRoomFor(rewards))
            {
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.NotEnoughSpace });
                return;
            }

            if (!mapInstance.TryRemoveMapItem(evt.VisualId))
            {
                return;
            }

            var rest = LeaveTheRest(mapInstance, boxes, box, spot);

            // Every reward is placed before anything is sent, so nothing awaited can take its room or
            // leave it undelivered; what the bag still refuses stays on the ground for the opener.
            var received = new List<(short VNum, short Amount, IvnPacket PocketChange)>();
            var leftOnTheGround = new List<MapItemComponentBundle>();
            foreach (var reward in rewards)
            {
                var vNum = reward.ItemVNum;
                var (placed, left, changed) = inventory.AddAsMuchAsFits(reward, character.CharacterId);
                if (placed > 0 && changed.GeneratePocketChange() is { } pocketChange)
                {
                    received.Add((vNum, placed, pocketChange));
                }

                if (left > 0)
                {
                    leftOnTheGround.Add(mapItemProvider.Create(mapInstance, reward, spot.PositionX, spot.PositionY, character.CharacterId));
                }
            }

            await mapInstance.SendPacketAsync(opened);
            foreach (var (vNum, _, pocketChange) in received)
            {
                await session.SendPacketAsync(pocketChange);
                await session.SendPacketAsync(character.GenerateIcon(1, vNum));
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 1,
                    Game18NArguments = { vNum }
                });
            }

            await mapInstance.SendPacketAsync(evt.GenerateGet());
            await ShowAsync(mapInstance, rest);
            foreach (var dropped in leftOnTheGround)
            {
                await mapInstance.SendPacketAsync(dropped.GenerateDrop());
            }

            foreach (var (vNum, amount, _) in received)
            {
                await gameEvents.RaiseAsync(new ItemsReceivedEvent(character, vNum, amount));
            }
        }

        // One box of a dropped stack is opened at a time; the others go back on the ground as a new
        // stack, shown once the opened one is gone.
        private MapItemComponentBundle? LeaveTheRest(MapInstance mapInstance, short boxes, IItemInstance box,
            (short X, short Y, long? OwnerId) spot)
        {
            if (boxes <= 1 || box.Clone() is not IItemInstance rest)
            {
                return null;
            }

            rest.Id = Guid.NewGuid();
            rest.Amount = (short)(boxes - 1);
            return mapItemProvider.Create(mapInstance, rest, spot.X, spot.Y, spot.OwnerId);
        }

        private static Task ShowAsync(MapInstance mapInstance, MapItemComponentBundle? item) =>
            item is { } shown ? mapInstance.SendPacketAsync(shown.GenerateDrop()) : Task.CompletedTask;
    }
}
