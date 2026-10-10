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
using NosCore.GameObject.Services.DigestionService;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class FoodHandler(IClock clock, ICardCatalog cardCatalog)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            var session = evt.ClientSession;
            var character = session.Character;
            if (item.ItemType != ItemType.Food || (item.Hp <= 0 && item.Mp <= 0)
                || cardCatalog.GetItemBCards(item.VNum).Count > 0 || !character.IsAlive)
            {
                return;
            }

            // Set rather than toggled: two meals handled at once must not stand the character back up.
            if (!character.IsSitting)
            {
                character.IsSitting = true;
                await character.MapInstance.SendPacketAsync(character.GenerateRest());
            }

            await character.MapInstance.SendPacketAsync(character.GenerateEff(DigestionService.EatEffect));
            var itemInstance = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange((PocketType)itemInstance.Type, itemInstance.Slot));

            var hpPerTick = item.Hp / DigestionSchedule.TickCount;
            var mpPerTick = item.Mp / DigestionSchedule.TickCount;
            var state = character.Digestion;
            bool full;

            await state.Lock.WaitAsync();
            try
            {
                full = !state.Food.IsEmpty && state.Food.WouldExceed(hpPerTick, mpPerTick, DigestionService.FoodCap);
                if (state.Food.IsEmpty && state.Snack.IsEmpty)
                {
                    state.LastTick = clock.GetCurrentInstant();
                }

                state.Food.Add(hpPerTick, mpPerTick);
            }
            finally
            {
                state.Lock.Release();
            }

            if (full)
            {
                await session.SendPacketAsync(new SayiPacket
                {
                    VisualType = VisualType.Player,
                    VisualId = character.CharacterId,
                    Type = SayColorType.BubbleOnly,
                    Message = Game18NConstString.IAmFull
                });
            }
        }
    }
}
