//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NodaTime;
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
    public sealed class AncelloansBlessingHandler(IBuffService buffService, ICardCatalog cardCatalog)
    {
        public const short BlessingCardId = 121;

        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var session = evt.ClientSession;
            var character = session.Character;
            if (item.Effect != ItemEffectType.AncelloansBlessing || buffService.HasBuff(character, BlessingCardId)
                || cardCatalog.GetCard(BlessingCardId) is not { } card)
            {
                return;
            }

            await character.UseUpOneAsync(evt.InventoryItem);

            // A static buff's TIME column counts 0.6 s steps, so 6001 is an hour.
            await buffService.ApplyStaticAsync(character, card, cardCatalog.GetCardBCards(BlessingCardId),
                Duration.FromMilliseconds(card.Duration * 600L));
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
