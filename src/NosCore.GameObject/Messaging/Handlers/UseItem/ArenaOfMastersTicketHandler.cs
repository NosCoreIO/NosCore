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
using NosCore.Networking;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;

namespace NosCore.GameObject.Messaging.Handlers.UseItem
{
    [UsedImplicitly]
    public sealed class ArenaOfMastersTicketHandler
    {
        [UsedImplicitly]
        public async Task Handle(ItemUsedEvent evt)
        {
            if (evt.InventoryItem.ItemInstance.Item.Effect != ItemEffectType.ArenaOfMastersTicket)
            {
                return;
            }

            var character = evt.ClientSession.Character;
            character.CharacterDto.MasterTicket++;
            await character.UseUpOneAsync(evt.InventoryItem);
            await evt.ClientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.ArenaMastersEntriesIncreased,
            });
        }
    }
}
