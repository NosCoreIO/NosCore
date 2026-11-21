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
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class TarotCardGameHandler(IItemGenerationService itemProvider, IItemCatalog itemCatalog, IRandomProvider random)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var source = evt.InventoryItem;
            var game = source.ItemInstance.Item;
            var deck = itemCatalog.GetItemsWithEffect(ItemEffectType.SealedTarotCard);

            if (game.Effect != ItemEffectType.TarotCardGame || game.EffectValue <= 0 || deck.Count == 0)
            {
                return;
            }

            var dealt = Enumerable.Range(0, game.EffectValue)
                .Select(_ => deck[random.Next(0, deck.Count)].VNum)
                .GroupBy(vnum => vnum)
                .Select(g => itemProvider.Create(g.Key, (short)g.Count()))
                .ToList();

            var session = evt.ClientSession;
            var character = session.Character;
            var inventory = character.InventoryService;
            if (!inventory.HasRoomFor(dealt))
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            var received = dealt.Select(card => (card.ItemVNum, card.Amount)).ToList();
            var granted = inventory.ConsumeAndAdd(source, 1, dealt
                .Select(card => InventoryItemInstance.Create(card, character.CharacterId)).ToList());
            if (granted == null)
            {
                return;
            }

            if (granted.Count == 0)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Yellow, Game18NConstString.NotEnoughSpace));
                return;
            }

            foreach (var added in granted)
            {
                await session.SendPacketAsync(added.GeneratePocketChange(added.Type.ToPocketType(), added.Slot));
            }

            foreach (var (vNum, amount) in received)
            {
                await session.SendPacketAsync(character.GenerateSayi(SayColorType.Green, Game18NConstString.ReceivedThisItem,
                    2, vNum.ToString(), amount));
            }

            await session.SendPacketAsync(inventory.LoadBySlotAndType(source.Slot, source.Type)
                .GeneratePocketChange(source.Type.ToPocketType(), source.Slot));
        }
    }
}
