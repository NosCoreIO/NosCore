//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
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
    public sealed class FamilyExperienceBoosterHandler(IClock clock)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            var item = source.ItemInstance.Item;
            if (item.Effect != ItemEffectType.FamilyExperienceBooster)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            var duration = Duration.FromDays(item.EffectValue);
            // Another booster used while one runs adds its days to it.
            if (character.StaticBonusList.FirstOrDefault(s => s.StaticBonusType == StaticBonusType.FamilyExperienceBooster)
                is { DateEnd: { } end } running)
            {
                running.DateEnd = end.Plus(duration);
            }
            else
            {
                character.StaticBonusList.Add(new StaticBonusDto
                {
                    CharacterId = character.CharacterId,
                    DateEnd = clock.GetCurrentInstant().Plus(duration),
                    StaticBonusType = StaticBonusType.FamilyExperienceBooster
                });
            }

            await character.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.EffectActivated,
                ArgumentType = 2,
                Game18NArguments = { item.VNum.ToString() }
            });
            await character.UseUpOneAsync(source);
        }
    }
}
