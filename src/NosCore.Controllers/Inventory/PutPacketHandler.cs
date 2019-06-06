using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.GameObject.Providers.ItemProvider.Item;

namespace NosCore.PacketHandlers.Inventory
{
    public class PutPacketHandler : PacketHandler<PutPacket>, IWorldPacketHandler
    {
        private readonly WorldConfiguration _worldConfiguration;
        public PutPacketHandler(WorldConfiguration worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        public override void Execute(PutPacket putPacket, ClientSession clientSession)
        {
            var invitem =
                    clientSession.Character.Inventory.LoadBySlotAndType(putPacket.Slot, putPacket.NoscorePocketType);
            if ((invitem?.ItemInstance.Item.IsDroppable ?? false) && !clientSession.Character.InExchangeOrShop)
            {
                if (putPacket.Amount > 0 && putPacket.Amount <= _worldConfiguration.MaxItemAmount)
                {
                    if (clientSession.Character.MapInstance.MapItems.Count < 200)
                    {
                        var droppedItem =
                            clientSession.Character.MapInstance.PutItem(putPacket.Amount, invitem.ItemInstance, clientSession);
                        if (droppedItem == null)
                        {
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                                    clientSession.Account.Language),
                                Type = 0
                            });
                            return;
                        }

                        invitem = clientSession.Character.Inventory.LoadBySlotAndType(putPacket.Slot,
                            putPacket.NoscorePocketType);
                        clientSession.SendPacket(invitem.GeneratePocketChange(putPacket.NoscorePocketType, putPacket.Slot));
                        clientSession.Character.MapInstance.Sessions.SendPacket(droppedItem.GenerateDrop());
                    }
                    else
                    {
                        clientSession.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.DROP_MAP_FULL,
                                clientSession.Account.Language),
                            Type = 0
                        });
                    }
                }
                else
                {
                    clientSession.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_DROP_AMOUNT,
                            clientSession.Account.Language),
                        Type = 0
                    });
                }
            }
            else
            {
                clientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                        clientSession.Account.Language),
                    Type = 0
                });
            }
        }
    }
}
