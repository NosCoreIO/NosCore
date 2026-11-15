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
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class MedalHandler(IOptions<WorldConfiguration> conf, IClock clock)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.ItemType != ItemType.Special || item.Effect.MedalBonus() is not { } bonus)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;
            var refusal = bonus switch
            {
                StaticBonusType.MedalOfErenia or StaticBonusType.AdventurersMedal
                    when character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack) => Game18NConstString.NotWithKnapsack,
                StaticBonusType.MedalOfErenia when character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.AdventurersMedal)
                    => Game18NConstString.NoMedalEreniaAndAdventurerTogether,
                StaticBonusType.AdventurersMedal when character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.MedalOfErenia)
                    => Game18NConstString.NoMedalEreniaAndAdventurerTogether,
                _ => (Game18NConstString?)null,
            };
            if (refusal is { } message)
            {
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = message });
                return;
            }

            var days = Duration.FromDays(item.EffectValue);
            var active = character.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == bonus);
            if (active != null)
            {
                if (evt.Packet.Mode == 0)
                {
                    await session.SendPacketAsync(new QnaiPacket
                    {
                        YesPacket = character.GenerateUseItem(evt.InventoryItem.Type.ToPocketType(), evt.InventoryItem.Slot, 1, 0),
                        Question = Game18NConstString.AskExtendMedal
                    });
                    return;
                }

                active.DateEnd = (active.DateEnd ?? clock.GetCurrentInstant()).Plus(days);
                await character.UseUpOneAsync(evt.InventoryItem);
                await session.SendPacketAsync(new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.MedalUsablePeriodChanged });
                return;
            }

            character.StaticBonusList.Add(new StaticBonusDto
            {
                CharacterId = character.CharacterId,
                DateEnd = clock.GetCurrentInstant().Plus(days),
                StaticBonusType = bonus
            });
            await character.UseUpOneAsync(evt.InventoryItem);
            await session.SendPacketAsync(bonus switch
            {
                StaticBonusType.MedalOfErenia => new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.EreniaMedalActivated },
                StaticBonusType.AdventurersMedal => new MsgiPacket { Type = MessageType.Default, Message = Game18NConstString.AdventurerMedalActivated },
                _ => new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.EffectActivated,
                    ArgumentType = 2,
                    Game18NArguments = { item.VNum.ToString() }
                },
            });

            character.LoadExpensions();
            await session.SendPacketAsync(character.GenerateExts(conf));
        }
    }
}
