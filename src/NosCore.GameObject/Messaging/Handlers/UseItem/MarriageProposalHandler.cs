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
using NosCore.GameObject.InterChannelCommunication.Hubs.FriendHub;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using ClientGuriPacket = NosCore.Packets.ClientPackets.UI.GuriPacket;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class MarriageProposalHandler(IFriendHub friendHub, ISessionRegistry sessionRegistry)
    {
        private const byte Confirmed = 2;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var itemInstance = evt.InventoryItem;
            if (itemInstance.ItemInstance.Item.Effect != ItemEffectType.MarriageProposal)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var target = evt.Packet.VisualId == character.CharacterId
                ? null
                : sessionRegistry.GetSessionByCharacterId(evt.Packet.VisualId)?.Character;
            if (target is not { } spouse)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SelectAppropriateTarget
                });
                return;
            }

            var friends = await friendHub.GetFriendsAsync(character.CharacterId);
            if (friends.Any(f => f.RelationType == CharacterRelationType.Spouse))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.AlreadyMarried
                });
                return;
            }

            if (!friends.Any(f => f.CharacterId == spouse.CharacterId && f.RelationType == CharacterRelationType.Friend))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.SelectAppropriateTarget
                });
                return;
            }

            if ((await friendHub.GetFriendsAsync(spouse.CharacterId)).Any(f => f.RelationType == CharacterRelationType.Spouse))
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.PlayerAlreadyMarried
                });
                return;
            }

            if (evt.Packet.Mode != Confirmed)
            {
                await session.SendPacketAsync(new Qnai2Packet
                {
                    YesPacket = new UseItemPacket
                    {
                        VisualType = VisualType.Player,
                        VisualId = spouse.CharacterId,
                        Type = itemInstance.Type.ToPocketType(),
                        Slot = itemInstance.Slot,
                        Mode = Confirmed
                    },
                    Question = Game18NConstString.AskPropose,
                    ArgumentType = 1,
                    Game18NArguments = { spouse.Name }
                });
                return;
            }

            character.MarriageProposalCharacterIds[spouse.CharacterId] = spouse.CharacterId;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));
            await session.SendPacketAsync(new Msgi2Packet
            {
                Type = MessageType.Default,
                Message = Game18NConstString.HaveProposed,
                ArgumentType = 1,
                Game18NArguments = { spouse.Name }
            });
            await spouse.SendPacketAsync(new Dlgi2Packet
            {
                YesPacket = new ClientGuriPacket
                {
                    Type = GuriPacketType.Wedding,
                    Argument = 1,
                    VisualId = character.CharacterId
                },
                NoPacket = new ClientGuriPacket
                {
                    Type = GuriPacketType.Wedding,
                    Argument = 0,
                    VisualId = character.CharacterId
                },
                Question = Game18NConstString.AskAcceptPropose,
                ArgumentType = 1,
                Game18NArguments = { character.Name }
            });
        }
    }
}
