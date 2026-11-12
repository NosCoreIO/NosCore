//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.InterChannelCommunication.Hubs.ChannelHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.InterChannelCommunication.Hubs.PubSub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SeparationLetterHandler(IFriendHub friendHub, IChannelHub channelHub, IPubSubHub pubSubHub,
        ISessionRegistry sessionRegistry, ICoupleBuffService coupleBuffService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var itemInstance = evt.InventoryItem;
            if (itemInstance.ItemInstance.Item.Effect != ItemEffectType.MarriageSeparation)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (!(await friendHub.GetFriendsAsync(character.CharacterId)).Any(f => f.RelationType == CharacterRelationType.Spouse))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CantBeUsed
                });
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = character.GenerateUseItem(itemInstance.Type.ToPocketType(), itemInstance.Slot, 1, 0),
                    Question = Game18NConstString.AskSeparateSpouse
                });
                return;
            }

            if (await friendHub.SeparateAsync(character.CharacterId) is not { } spouseId)
            {
                return;
            }

            await character.UseUpOneAsync(itemInstance);
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.SpouseSeparated
            });
            await session.SendPacketAsync(await character.GenerateFinitAsync(friendHub, channelHub, pubSubHub));
            if (sessionRegistry.GetSessionByCharacterId(spouseId)?.Character is { } spouse)
            {
                await spouse.SendPacketAsync(await spouse.GenerateFinitAsync(friendHub, channelHub, pubSubHub));
                await coupleBuffService.RefreshAsync([spouse]);
            }

            await coupleBuffService.RefreshAsync([character]);
        }
    }
}
