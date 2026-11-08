//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
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
            var character = clientSession.Character;
            if (character.InExchangeOrShop)
            {
                return;
            }

            var cardItem = character.InventoryService.LoadBySlotAndType(packet.Slot, NoscorePocketType.Equipment);
            var holderItem = character.InventoryService.LoadBySlotAndType(packet.HolderSlot, NoscorePocketType.Equipment);
            if (cardItem?.ItemInstance is not SpecialistInstance card
                || holderItem == null
                || !specialistHolderService.IsSpecialistHolder(holderItem.ItemInstance)
                || holderItem.ItemInstance is not BoxInstance { HoldingVNum: null or 0 } holder)
            {
                return;
            }

            if (character.UseSp)
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.CantPutSpecialistInHolderWhileTransformed
                });
                return;
            }

            if (!specialistHolderService.CanStore(card))
            {
                await clientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.NotBeAbleToPutTheSelectedSpecialistCardIntoCardHolder
                });
                return;
            }

            specialistHolderService.Store(card, holder);
            character.InventoryService.RemoveItemAmountFromInventory(1, cardItem.ItemInstanceId);
            await clientSession.SendPacketAsync(cardItem.GeneratePocketChange((PocketType)cardItem.Type, cardItem.Slot));
            await clientSession.SendPacketAsync(holderItem.GeneratePocketChange((PocketType)holderItem.Type, holderItem.Slot));
            await clientSession.SendPacketAsync(new ShopEndPacket { Type = ShopEndPacketType.CloseSubWindow });
            await clientSession.SendPacketAsync(new MsgiPacket
            {
                Type = MessageType.Default,
                Message = Game18NConstString.SpecialistInTheHolder
            });
        }
    }
}
