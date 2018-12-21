//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Handling;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.Enumerations.Map;
using NosCore.Shared.I18N;
using Serilog;
using System;
using System.Linq;

namespace NosCore.GameObject.Services.MapInstanceAccess.Handling
{
    public class DropHandler : IHandler<MapItem, Tuple<MapItem, GetPacket>>
    {
        public bool Condition(MapItem item) => item.ItemInstance.Item.ItemType != ItemType.Map && item.VNum != 1046;

        public void Execute(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            var amount = requestData.Data.Item1.Amount;
            var inv = requestData.ClientSession.Character.Inventory.AddItemToPocket(requestData.Data.Item1.ItemInstance).FirstOrDefault();

            if (inv != null)
            {
                requestData.ClientSession.SendPacket(inv.GeneratePocketChange(inv.Type, inv.Slot));
                requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out var value);
                requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
                if (requestData.Data.Item2.PickerType == PickerType.Mate)
                {
                    requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateIcon(1, inv.ItemVNum));
                }

                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, requestData.ClientSession.Account.Language)}: {inv.Item.Name} x {amount}",
                    SayColorType.Green));
                if (requestData.ClientSession.Character.MapInstance.MapInstanceType == MapInstanceType.LodInstance)
                {
                    var name = string.Format(
                        Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED_LOD,
                            requestData.ClientSession.Account.Language), requestData.ClientSession.Character.Name);
                    requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(requestData.ClientSession.Character.GenerateSay(
                        $"{name}: {inv.Item.Name} x {requestData.Data.Item1.Amount}",
                        SayColorType.Yellow));
                }
            }
            else
            {
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
                        requestData.ClientSession.Account.Language),
                    Type = 0
                });
            }
        }
    }
}
