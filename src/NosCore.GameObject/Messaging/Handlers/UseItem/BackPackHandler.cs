//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NodaTime;
using NosCore.Core.Configuration;
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
    public sealed class BackPackHandler(IOptions<WorldConfiguration> conf, IClock clock)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var itemInstance = evt.InventoryItem;
            var effect = itemInstance.ItemInstance.Item.Effect;
            if (effect != ItemEffectType.InventoryUpgrade && effect != ItemEffectType.InventoryTicketUpgrade)
            {
                return;
            }

            var session = evt.ClientSession;
            if (effect == ItemEffectType.InventoryUpgrade
                && session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack))
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.NotInPair
                });
                return;
            }

            if (effect == ItemEffectType.InventoryTicketUpgrade
                && session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.InventoryTicketUpgrade))
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = session.Character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.NotInPair
                });
                return;
            }

            session.Character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = session.Character.CharacterId,
                DateEnd = itemInstance.ItemInstance.Item.EffectValue == 0
                    ? null
                    : clock.GetCurrentInstant().Plus(Duration.FromDays(itemInstance.ItemInstance.Item.EffectValue)),
                StaticBonusType = effect == ItemEffectType.InventoryTicketUpgrade
                    ? StaticBonusType.InventoryTicketUpgrade
                    : StaticBonusType.BackPack
            });

            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = session.Character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.EffectActivated,
                ArgumentType = 2,
                Game18NArguments = { itemInstance.ItemInstance.Item.VNum.ToString() }
            });
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));
            session.Character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);

            session.Character.LoadExpensions();
            await session.SendPacketAsync(session.Character.GenerateExts(conf));
        }
    }
}
