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

using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.InventoryService;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Networking;

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class DropEventHandler : IGetMapItemEventHandler
    {
        public bool Condition(MapItem item)
        {
            return (item.ItemInstance!.Item!.ItemType != ItemType.Map) && (item.VNum != 1046);
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            var amount = requestData.Data.Item1.Amount;
            var iteminstance = InventoryItemInstance.Create(requestData.Data.Item1.ItemInstance!,
                requestData.ClientSession.Character.CharacterId);
            var inv = requestData.ClientSession.Character.InventoryService.AddItemToPocket(iteminstance)?
                .FirstOrDefault();

            if (inv != null)
            {
                await requestData.ClientSession.SendPacketAsync(inv.GeneratePocketChange((PocketType)inv.Type, inv.Slot)).ConfigureAwait(false);
                requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId,
                    out _);
                await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                    requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId)).ConfigureAwait(false);
                if (requestData.Data.Item2.PickerType == VisualType.Npc)
                {
                    await requestData.ClientSession.SendPacketAsync(
                        requestData.ClientSession.Character.GenerateIcon(1, inv.ItemInstance!.ItemVNum)).ConfigureAwait(false);
                }

                await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateSay(
                    $"{GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, requestData.ClientSession.Account.Language)}: {inv.ItemInstance!.Item!.Name[requestData.ClientSession.Account.Language]} x {amount}",
                    SayColorType.Green)).ConfigureAwait(false);
                if (requestData.ClientSession.Character.MapInstance.MapInstanceType == MapInstanceType.LodInstance)
                {
                    var name = string.Format(
                        GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED_LOD,
                            requestData.ClientSession.Account.Language), requestData.ClientSession.Character.Name);
                    await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                        requestData.ClientSession.Character.GenerateSay(
                            $"{name}: {inv.ItemInstance.Item.Name[requestData.ClientSession.Account.Language]} x {requestData.Data.Item1.Amount}",
                            SayColorType.Yellow)).ConfigureAwait(false);
                }
            }
            else
            {
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Message = Game18NConstString.NotEnoughSpace,
                    Type = 0
                }).ConfigureAwait(false);
            }
        }
    }
}