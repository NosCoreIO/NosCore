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
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class SpeedBoosterHandler(IBuffService buffService, ICardCatalog cardCatalog,
        ISpeedCalculationService speedCalculationService)
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var session = evt.ClientSession;
            var character = session.Character;
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.SpeedBooster
                || !character.IsVehicled || character.VehicleSpeedBoost == 0
                || buffService.HasBuff(character, SpeedBooster.CardId)
                || cardCatalog.GetCard(SpeedBooster.CardId) is not { } card)
            {
                return;
            }

            var itemInstance = evt.InventoryItem;
            character.InventoryService.RemoveItemAmountFromInventory(1, itemInstance.ItemInstanceId);
            await session.SendPacketAsync(itemInstance.GeneratePocketChange(itemInstance.Type.ToPocketType(), itemInstance.Slot));

            await buffService.ApplyAsync(character, card, cardCatalog.GetCardBCards(SpeedBooster.CardId), character,
                character.VehicleSpeedBoostSeconds * 1000);
            character.Speed = speedCalculationService.CalculateSpeed(character);
            await character.MapInstance.SendPacketAsync(character.GenerateEff(SpeedBooster.Effect));
            await session.SendPacketAsync(character.GenerateCond());
        }
    }
}
