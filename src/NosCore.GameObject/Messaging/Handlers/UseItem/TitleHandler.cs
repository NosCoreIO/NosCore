//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.Messaging.Events;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class TitleHandler
    {
        [UsedImplicitly]
        public Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.ItemType != ItemType.Title)
            {
                return Task.CompletedTask;
            }

            return evt.ClientSession.SendPacketAsync(new QnaiPacket
            {
                YesPacket = new GuriPacket
                {
                    Type = GuriPacketType.Title,
                    Argument = (uint)evt.InventoryItem.ItemInstance.ItemVNum,
                    EntityId = evt.InventoryItem.Slot
                },
                Question = Game18NConstString.AskAddTitle
            });
        }
    }
}
