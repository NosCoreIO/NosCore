//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class CuarryBankVipMedalHandler(IClock clock)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var itemInstance = evt.InventoryItem;
            var item = itemInstance.ItemInstance.Item;
            if (item.Effect != ItemEffectType.CuarryBankVipMedal)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            if (character.HasBankVip())
            {
                return;
            }

            character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = character.CharacterId,
                DateEnd = clock.GetCurrentInstant().Plus(Duration.FromDays(item.EffectValue)),
                StaticBonusType = StaticBonusType.CuarryBankVip
            });
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.EffectActivated,
                ArgumentType = 2,
                Game18NArguments = { item.VNum.ToString() }
            });

            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
        }
    }
}
