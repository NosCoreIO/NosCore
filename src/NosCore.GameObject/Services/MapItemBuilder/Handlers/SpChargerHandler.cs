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
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.ClientPackets;
using NosCore.Packets.ServerPackets;
using NosCore.Shared.Enumerations.Items;
using NosCore.Shared.I18N;
using System;

namespace NosCore.GameObject.Services.MapItemBuilder.Handlers
{
    public class SpChargerHandler : IHandler<MapItem, Tuple<MapItem, GetPacket>>
    {
        public bool Condition(MapItem item) =>
            item.ItemInstance.Item.ItemType == ItemType.Map && item.ItemInstance.Item.Effect == Effect.SpCharger;

        public void Execute(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            requestData.ClientSession.Character.AddSpPoints(requestData.Data.Item1.ItemInstance.Item.EffectValue);

            requestData.ClientSession.SendPacket(new MsgPacket
            {
                Message = string.Format(
                    Language.Instance.GetMessageFromKey(LanguageKey.SP_POINTSADDED,
                        requestData.ClientSession.Account.Language),
                    requestData.Data.Item1.ItemInstance.Item.EffectValue),
                Type = 0
            });
            requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSpPoint());

            requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            requestData.ClientSession.Character.MapInstance.Sessions.SendPacket(
                requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
        }
    }
}