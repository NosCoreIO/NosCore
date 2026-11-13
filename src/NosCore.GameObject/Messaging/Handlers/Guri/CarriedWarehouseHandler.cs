//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.WarehouseService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Guri
{
    // The pet basket opens the miniland warehouse from anywhere; the partner backpack opens its own storage.
    [UsedImplicitly]
    public sealed class CarriedWarehouseHandler(IPersonalWarehouseService warehouseService)
    {
        [UsedImplicitly]
        public Task Handle(GuriPacketReceivedEvent evt)
        {
            var session = evt.ClientSession;
            return evt.Packet.Type switch
            {
                GuriPacketType.PetBasket when session.Character.StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBasket)
                    => warehouseService.OpenAsync(session, WarehouseType.Warehouse),
                GuriPacketType.PartnerBackpack => warehouseService.OpenAsync(session, WarehouseType.PetWarehouse),
                _ => Task.CompletedTask
            };
        }
    }
}
