//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class RenamingCardHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.RenamingCard)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;

            if (character.CharacterDto.ShouldRename)
            {
                await session.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.YouHaveAlreadyUsedThisItemToRenameThisCharacter
                });
                return;
            }

            if (evt.Packet.Mode == 0)
            {
                await session.SendPacketAsync(new QnaiPacket
                {
                    YesPacket = character.GenerateUseItem(evt.InventoryItem.Type.ToPocketType(), evt.InventoryItem.Slot, 1, 0),
                    Question = Game18NConstString.ThisArticleAllowsYouToChangeYourName
                });
                return;
            }

            character.CharacterDto.ShouldRename = true;
            await character.UseUpOneAsync(evt.InventoryItem);
            await session.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.ToChangeYourCharacterNameYouHaveToLogoutAndGoBackToInitial
            });
        }
    }
}
