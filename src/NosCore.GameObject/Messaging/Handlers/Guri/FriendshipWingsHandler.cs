//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.MapChangeService;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // Argument 1 is the friend list's Go button; argument 2 comes back after the delay bar.
    [UsedImplicitly]
    public sealed class FriendshipWingsHandler(IFriendHub friendHub, ISessionRegistry sessionRegistry,
        IMapChangeService mapChangeService, IClock clock)
    {
        private const int Request = 1;
        private const int Travel = 2;

        [UsedImplicitly]
        public async Task Handle(GuriPacketReceivedEvent evt)
        {
            var packet = evt.Packet;
            if (packet.Type != GuriPacketType.FriendshipWings
                || (packet.Argument != Request && packet.Argument != Travel)
                || packet.VisualId is not { } friendId)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var wings = character.InventoryService.Values
                .FirstOrDefault(i => i.ItemInstance.Item.Effect == ItemEffectType.FriendshipWings);
            if (wings == null)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.NoFriendshipWings
                });
                return;
            }

            var friends = await friendHub.GetFriendsAsync(character.CharacterId);
            if (!friends.Any(f => f.CharacterId == friendId && f.RelationType == CharacterRelationType.Friend))
            {
                return;
            }

            if (packet.Argument == Request)
            {
                await character.StartDelayAsync(new DelayPacket
                {
                    Delay = 3000,
                    Type = DelayPacketType.InUse,
                    Packet = new ClientGuriPacket
                    {
                        Type = GuriPacketType.FriendshipWings,
                        Argument = Travel,
                        VisualId = friendId
                    }
                }, friendId, clock.GetCurrentInstant());
                return;
            }

            if (!character.TryCompleteDelay(DelayPacketType.InUse, friendId, clock.GetCurrentInstant()))
            {
                return;
            }

            var friend = sessionRegistry.GetSessionByCharacterId(friendId)?.Character;
            if (friend == null)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.YourFriendIsOffline
                });
                return;
            }

            if (character.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance
                || friend.Value.MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Yellow,
                    Message = Game18NConstString.CanNotBeUsedHere
                });
                return;
            }

            character.InventoryService.RemoveItemAmountFromInventory(1, wings.ItemInstanceId);
            await session.SendPacketAsync(wings.GeneratePocketChange(wings.Type.ToPocketType(), wings.Slot));
            await mapChangeService.ChangeMapAsync(session, friend.Value.MapId, friend.Value.PositionX, friend.Value.PositionY);
        }
    }
}
