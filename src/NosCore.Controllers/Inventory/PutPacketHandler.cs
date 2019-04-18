using System;
using System.Collections.Generic;
using System.Text;
using ChickenAPI.Packets.ClientPackets.Inventory;
using ChickenAPI.Packets.Enumerations;
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

        public override void Execute(PutPacket putPacket, ClientSession session)
        {
            var invitem =
                    session.Character.Inventory.LoadBySlotAndType<IItemInstance>(putPacket.Slot, putPacket.PocketType);
            if ((invitem?.Item.IsDroppable ?? false) && !session.Character.InExchangeOrShop)
            {
                if (putPacket.Amount > 0 && putPacket.Amount <= _worldConfiguration.MaxItemAmount)
                {
                    if (session.Character.MapInstance.MapItems.Count < 200)
                    {
                        var droppedItem =
                            session.Character.MapInstance.PutItem(putPacket.Amount, invitem, session);
                        if (droppedItem == null)
                        {
                            session.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                                    session.Account.Language),
                                Type = 0
                            });
                            return;
                        }

                        invitem = session.Character.Inventory.LoadBySlotAndType<IItemInstance>(putPacket.Slot,
                            putPacket.PocketType);
                        session.SendPacket(invitem.GeneratePocketChange(putPacket.PocketType, putPacket.Slot));
                        session.Character.MapInstance.Sessions.SendPacket(droppedItem.GenerateDrop());
                    }
                    else
                    {
                        session.SendPacket(new MsgPacket
                        {
                            Message = Language.Instance.GetMessageFromKey(LanguageKey.DROP_MAP_FULL,
                                session.Account.Language),
                            Type = 0
                        });
                    }
                }
                else
                {
                    session.SendPacket(new MsgPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.BAD_DROP_AMOUNT,
                            session.Account.Language),
                        Type = 0
                    });
                }
            }
            else
            {
                session.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                        session.Account.Language),
                    Type = 0
                });
            }
        }
    }
    }
}
