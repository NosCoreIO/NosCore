//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class ChangeGenderHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.ChangeGender)
            {
                return;
            }

            var session = evt.ClientSession;
            var character = session.Character;

            if (character.IsVehicled || character.InventoryService.Values.Any(v => v.Type == NoscorePocketType.Wear))
            {
                return;
            }

            character.Gender = character.Gender == GenderType.Female ? GenderType.Male : GenderType.Female;

            await session.SendPacketAsync(character.GenerateEq());
            await character.MapInstance.SendPacketAsync(character.GenerateIn(string.Empty));
            await character.MapInstance.SendPacketAsync(character.GenerateCMode());
            await character.MapInstance.SendPacketAsync(character.GenerateEff(196));

            character.InventoryService.RemoveItemAmountFromInventory(1, evt.InventoryItem.ItemInstanceId);
            await session.SendPacketAsync(evt.InventoryItem.GeneratePocketChange((PocketType)evt.InventoryItem.Type, evt.InventoryItem.Slot));
        }
    }
}
