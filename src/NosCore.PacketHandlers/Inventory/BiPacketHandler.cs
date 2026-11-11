//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Inventory
{
    public class BiPacketHandler
        : PacketHandler<BiPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(BiPacket bIPacket, ClientSession clientSession)
        {
            switch (bIPacket.Option)
            {
                case null:
                    await clientSession.SendPacketAsync(
                        new DlgiPacket
                        {
                            YesPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Requested
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Declined
                            },
                            Question = Game18NConstString.ItemWillDestroy
                        });
                    break;

                case RequestDeletionType.Requested:
                    await clientSession.SendPacketAsync(
                        new DlgiPacket
                        {
                            YesPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Confirmed
                            },
                            NoPacket = new BiPacket
                            {
                                PocketType = bIPacket.PocketType,
                                Slot = bIPacket.Slot,
                                Option = RequestDeletionType.Declined
                            },
                            Question = Game18NConstString.AskDestroyItem
                        });
                    break;

                case RequestDeletionType.Confirmed:
                    var item = clientSession.Character.InventoryService.DeleteFromTypeAndSlot(
                        (NoscorePocketType)bIPacket.PocketType, bIPacket.Slot);
                    await clientSession.SendPacketAsync(item.GeneratePocketChange(bIPacket.PocketType, bIPacket.Slot));
                    break;
                default:
                    return;
            }
        }
    }
}
