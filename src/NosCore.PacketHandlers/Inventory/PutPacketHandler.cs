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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;
//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.PacketHandlers.Inventory
{
    public class PutPacketHandler : PacketHandler<PutPacket>, IWorldPacketHandler
    {
        private readonly IOptions<WorldConfiguration> _worldConfiguration;

        public PutPacketHandler(IOptions<WorldConfiguration> worldConfiguration)
        {
            _worldConfiguration = worldConfiguration;
        }

        public override async Task ExecuteAsync(PutPacket putPacket, ClientSession clientSession)
        {
            var invitem =
                clientSession.Character.InventoryService.LoadBySlotAndType(putPacket.Slot,
                    (NoscorePocketType)putPacket.PocketType);
            if (invitem?.ItemInstance?.Item?.IsDroppable ?? false)
            {
                if ((putPacket.Amount > 0) && (putPacket.Amount <= _worldConfiguration.Value.MaxItemAmount))
                {
                    if (clientSession.Character.MapInstance.MapItems.Count < 200)
                    {
                        var droppedItem =
                            clientSession.Character.MapInstance.PutItem(putPacket.Amount, invitem.ItemInstance,
                                clientSession);
                        if (droppedItem == null)
                        {
                            await clientSession.SendPacketAsync(new MsgPacket
                            {
                                Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE_HERE,
                                    clientSession.Account.Language),
                                Type = 0
                            }).ConfigureAwait(false);
                            return;
                        }

                        invitem = clientSession.Character.InventoryService.LoadBySlotAndType(putPacket.Slot,
                            (NoscorePocketType)putPacket.PocketType);
                        await clientSession.SendPacketAsync(invitem.GeneratePocketChange(putPacket.PocketType, putPacket.Slot)).ConfigureAwait(false);
                        await clientSession.Character.MapInstance.SendPacketAsync(droppedItem.GenerateDrop()).ConfigureAwait(false);
                    }
                    else
                    {
                        await clientSession.SendPacketAsync(new MsgPacket
                        {
                            Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.DROP_MAP_FULL,
                                clientSession.Account.Language),
                            Type = 0
                        }).ConfigureAwait(false);
                    }
                }
                else
                {
                    await clientSession.SendPacketAsync(new MsgPacket
                    {
                        Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.BAD_DROP_AMOUNT,
                            clientSession.Account.Language),
                        Type = 0
                    }).ConfigureAwait(false);
                }
            }
            else
            {
                await clientSession.SendPacketAsync(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_NOT_DROPPABLE,
                        clientSession.Account.Language),
                    Type = 0
                }).ConfigureAwait(false);
            }
        }
    }
}