using ChickenAPI.Packets.ClientPackets.Player;
using ChickenAPI.Packets.Enumerations;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider.Item;
using Serilog;

namespace NosCore.PacketHandlers.Inventory
{
    public class UpgradePacketHandler : PacketHandler<UpgradePacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public UpgradePacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override void Execute(UpgradePacket packet, ClientSession clientSession)
        {
            switch (packet.UpgradeType)
            {
                case UpgradePacketType.ItemToPart:
                    break;
                case UpgradePacketType.UpgradeItem:
                    break;
                case UpgradePacketType.CellonItem:
                    break;
                case UpgradePacketType.RarifyItem:
                    break;
                case UpgradePacketType.SumResistance:
                    var receiverItem = clientSession.Character.Inventory.LoadBySlotAndType(packet.Slot, (NoscorePocketType)packet.InventoryType);
                    if (receiverItem == null)
                    {
                        return;
                    }

                    if (receiverItem.ItemInstance is WearableInstance wearableReceiver)
                    {
                        if ((packet.Slot2 == null) || (packet.InventoryType2 == null))
                        {
                            return;
                        }

                        var sumItem = clientSession.Character.Inventory.LoadBySlotAndType((byte)packet.Slot2, (NoscorePocketType)packet.InventoryType2);
                        if (sumItem == null)
                        {
                            return;
                        }

                        if (sumItem.ItemInstance is WearableInstance wearableSum)
                        {
                            wearableReceiver.Sum(clientSession, wearableSum);
                        }
                    }
                    break;
                case UpgradePacketType.UpgradeItemProtected:
                    break;
                case UpgradePacketType.RarifyItemProtected:
                    break;
                case UpgradePacketType.UpgradeItemGoldScroll:
                    break;
                case UpgradePacketType.FusionItem:
                    break;
                case UpgradePacketType.UpgradeSpNoProtection:
                    break;
                case UpgradePacketType.UpgradeSpProtected:
                    break;
                case UpgradePacketType.UpgradeSpProtected2:
                    break;
                case UpgradePacketType.PerfectSp:
                    break;
                case UpgradePacketType.UpgradeSpChiken:
                    break;
                case UpgradePacketType.UpgradeSpPyjama:
                    break;
                case UpgradePacketType.UpgradeSpPirate:
                    break;
                case UpgradePacketType.CreateFairyFernon:
                    break;
                case UpgradePacketType.CreateFairyErenia:
                    break;
                case UpgradePacketType.CreateFairyZenas:
                    break;
                default:
                    _logger.Error(string.Format(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.UNKNOWN_UPGRADE_TYPE), (int)packet.UpgradeType));
                    return;
            }
        }
    }
}
