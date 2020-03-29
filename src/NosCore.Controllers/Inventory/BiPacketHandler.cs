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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Inventory;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using Serilog;

namespace NosCore.PacketHandlers.Inventory
{
    public class BiPacketHandler : PacketHandler<BiPacket>, IWorldPacketHandler
    {
        private readonly ILogger _logger;

        public BiPacketHandler(ILogger logger)
        {
            _logger = logger;
        }

        public override Task ExecuteAsync(BiPacket bIPacket, ClientSession clientSession)
        {
            switch (bIPacket.Option)
            {
                case null:
                    clientSession.SendPacketAsync(
                        new DlgPacket
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
                            Question = GameLanguage.Instance.GetMessageFromKey(LanguageKey.ASK_TO_DELETE,
                                clientSession.Account.Language)
                        });
                    break;

                case RequestDeletionType.Requested:
                    clientSession.SendPacketAsync(
                        new DlgPacket
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
                            Question = GameLanguage.Instance.GetMessageFromKey(LanguageKey.SURE_TO_DELETE,
                                clientSession.Account.Language)
                        });
                    break;

                case RequestDeletionType.Confirmed:
                    if (clientSession.Character.InExchangeOrShop)
                    {
                        _logger.Error(LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.CANT_MOVE_ITEM_IN_SHOP));
                        return Task.CompletedTask;
                    }

                    var item = clientSession.Character.InventoryService.DeleteFromTypeAndSlot(
                        (NoscorePocketType) bIPacket.PocketType, bIPacket.Slot);
                    clientSession.SendPacketAsync(item.GeneratePocketChange(bIPacket.PocketType, bIPacket.Slot));
                    break;
                default:
                    return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
    }
}