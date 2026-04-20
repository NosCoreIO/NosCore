//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking;
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Helpers;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class HairDieHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            var item = evt.InventoryItem.ItemInstance.Item;
            if (item.Effect != ItemEffectType.ApplyHairDie)
            {
                return;
            }

            var session = evt.ClientSession;
            if (session.Character.IsVehicled)
            {
                return;
            }

            var next = item.EffectValue == 99
                ? (byte)RandomHelper.Instance.RandomNumber(0, 128)
                : (byte)item.EffectValue;
            session.Character.HairColor = Enum.IsDefined(typeof(HairColorType), next)
                ? (HairColorType)next
                : HairColorType.DarkPurple;

            await session.SendPacketAsync(session.Character.GenerateEq());
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateIn(string.Empty));

            session.Character.InventoryService.RemoveItemAmountFromInventory(1, evt.InventoryItem.ItemInstanceId);
            await session.SendPacketAsync(evt.InventoryItem.GeneratePocketChange((PocketType)evt.InventoryItem.Type, evt.InventoryItem.Slot));
        }
    }
}
