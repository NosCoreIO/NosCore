//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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

                await mapInstance.SendPacketAsync(opened);
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.BoxIsEmpty });
                await mapInstance.SendPacketAsync(evt.GenerateGet());
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

            // Every reward is placed before anything is sent, so nothing awaited can take its room or
            // leave it undelivered; one the bag still refuses stays on the ground for the opener.
            var received = new List<(IItemInstance Reward, IvnPacket PocketChange)>();
            var leftOnTheGround = new List<MapItemComponentBundle>();
            foreach (var reward in rewards)
            {
                if ((inventory.AddItemToPocket(InventoryItemInstance.Create(reward, character.CharacterId)) ?? []).GeneratePocketChange() is { } pocketChange)
                {
                    received.Add((reward, pocketChange));
                    continue;
                }

                leftOnTheGround.Add(mapItemProvider.Create(mapInstance, reward, lying.PositionX, lying.PositionY, character.CharacterId));
            }

            await mapInstance.SendPacketAsync(opened);
            foreach (var (reward, pocketChange) in received)
            {
                await session.SendPacketAsync(pocketChange);
                await session.SendPacketAsync(character.GenerateIcon(1, reward.ItemVNum));
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 1,
                    Game18NArguments = { reward.ItemVNum }
                });
            }

            await mapInstance.SendPacketAsync(evt.GenerateGet());
            foreach (var dropped in leftOnTheGround)
            {
                await mapInstance.SendPacketAsync(dropped.GenerateDrop());
            }

            foreach (var (reward, _) in received)
            {
                await gameEvents.RaiseAsync(new ItemsReceivedEvent(character, reward.ItemVNum, reward.Amount));
            }
        }
    }
}
