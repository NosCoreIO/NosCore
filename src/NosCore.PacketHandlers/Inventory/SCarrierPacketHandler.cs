//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.SpecialistHolderService;
using NosCore.Packets.ClientPackets.Specialists;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Shop;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class SCarrierPacketHandler(ISpecialistHolderService specialistHolderService)
        : PacketHandler<SCarrierPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(SCarrierPacket packet, ClientSession clientSession)
        {
            var result = specialistHolderService.StoreFromEquipment(clientSession.Character, packet.Slot, packet.HolderSlot);
            switch (result.Outcome)
            {
                case SpecialistStoreOutcome.CannotStore:
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.NotBeAbleToPutTheSelectedSpecialistCardIntoCardHolder
                    });
                    return;

                case SpecialistStoreOutcome.Stored:
                    var card = result.Card!;
                    var holder = result.Holder!;
                    await clientSession.SendPacketAsync(result.Remaining.GeneratePocketChange(card.Type.ToPocketType(), card.Slot));
                    await clientSession.SendPacketAsync(holder.GeneratePocketChange(holder.Type.ToPocketType(), holder.Slot));
                    await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow });
                    await clientSession.SendPacketAsync(new MsgiPacket
                    {
                        Type = MessageType.Default,
                        Message = Game18NConstString.SpecialistInTheHolder
                    });
                    return;
            }
        }
    }
}
