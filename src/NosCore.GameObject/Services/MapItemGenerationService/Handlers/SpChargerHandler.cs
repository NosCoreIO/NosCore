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
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.ServerPackets.UI;
using System;
using System.Threading.Tasks;
using NosCore.Core.Networking;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class SpChargerEventHandler : IGetMapItemEventHandler
    {
        public bool Condition(MapItem item)
        {
            return (item.ItemInstance!.Item!.ItemType == ItemType.Map) &&
                (item.ItemInstance.Item.Effect == ItemEffectType.SpCharger);
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            await requestData.ClientSession.Character.AddSpPointsAsync(requestData.Data.Item1.ItemInstance!.Item!.EffectValue).ConfigureAwait(false);

            await requestData.ClientSession.SendPacketAsync(new MsgPacket
            {
                Message = string.Format(
                    GameLanguage.Instance.GetMessageFromKey(LanguageKey.SP_POINTSADDED,
                        requestData.ClientSession.Account.Language),
                    requestData.Data.Item1.ItemInstance.Item.EffectValue),
                Type = 0
            }).ConfigureAwait(false);
            await requestData.ClientSession.SendPacketAsync(requestData.ClientSession.Character.GenerateSpPoint()).ConfigureAwait(false);

            requestData.ClientSession.Character.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            await requestData.ClientSession.Character.MapInstance.SendPacketAsync(
                requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId)).ConfigureAwait(false);
        }
    }
}