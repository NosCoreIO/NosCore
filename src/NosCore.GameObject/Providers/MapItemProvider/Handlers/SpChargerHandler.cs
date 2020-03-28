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

using System;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Items;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.GameObject.Providers.MapItemProvider.Handlers
{
    public class SpChargerEventHandler : IEventHandler<MapItem, Tuple<MapItem, GetPacket>>
    {
        public bool Condition(MapItem item)
        {
            return (item.ItemInstance.Item.ItemType == ItemType.Map) &&
                (item.ItemInstance.Item.Effect == ItemEffectType.SpCharger);
        }

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
            requestData.ClientSession.Character.MapInstance.SendPacket(
                requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
        }
    }
}