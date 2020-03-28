//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

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
                clientSession.Character.InventoryService.LoadBySlotAndType(putPacket.Slot,
                    (NoscorePocketType) putPacket.PocketType);
            if ((invitem?.ItemInstance.Item.IsDroppable ?? false) && !clientSession.Character.InExchangeOrShop)
            {
                if ((putPacket.Amount > 0) && (putPacket.Amount <= _worldConfiguration.MaxItemAmount))
                {
                    if (clientSession.Character.MapInstance.MapItems.Count < 200)
                    {
                        var droppedItem =
                            clientSession.Character.MapInstance.PutItem(putPacket.Amount, invitem.ItemInstance,
                                clientSession);
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

                        invitem = clientSession.Character.InventoryService.LoadBySlotAndType(putPacket.Slot,
                            (NoscorePocketType) putPacket.PocketType);
                        clientSession.SendPacket(invitem.GeneratePocketChange(putPacket.PocketType, putPacket.Slot));
                        clientSession.Character.MapInstance.SendPacket(droppedItem.GenerateDrop());
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