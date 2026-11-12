//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class HeatResistanceHandler(IBuffService buffService, ICardCatalog cardCatalog, IClock clock,
        IBattleStatsProvider battleStatsProvider, IHeatService heatService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var session = evt.ClientSession;
            var character = session.Character;
            if (item.Effect != ItemEffectType.HeatResistance
                || cardCatalog.GetItemCard(item.VNum) is not { } card)
            {
                return;
            }

            var current = buffService.GetActiveBuffs(character).FirstOrDefault(b => b.CardId == card.CardId);
            var left = current == null ? Duration.Zero : current.ExpiresAt - clock.GetCurrentInstant();
            // A static buff's TIME column counts 0.6 s steps.
            var maximum = Duration.FromMilliseconds(card.Duration * 600L);
            if (left >= maximum)
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);

            var duration = Duration.Min(left + Duration.FromSeconds(item.EffectValue), maximum);
            await buffService.ApplyStaticAsync(character, card, cardCatalog.GetCardBCards(card.CardId), duration);
            await heatService.RefreshAsync(character);
            await session.SendPacketAsync(character.GenerateSc(battleStatsProvider));
            await session.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = character.CharacterId,
                Type = SayColorType.Green,
                Message = Game18NConstString.EffectActivated,
                ArgumentType = 2,
                Game18NArguments = { item.VNum.ToString() }
            });
        }
    }
}
