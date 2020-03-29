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
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;

namespace NosCore.GameObject.Providers.MapItemProvider.Handlers
{
    public class GoldDropEventHandler : IEventHandler<MapItem, Tuple<MapItem, GetPacket>>
    {
        public bool Condition(MapItem item)
        {
            return item.VNum == 1046;
        }

        public async Task Execute(RequestData<Tuple<MapItem, GetPacket>> requestData)
        {
            // handle gold drop
            var maxGold = requestData.ClientSession.WorldConfiguration.MaxGoldAmount;
            if (requestData.ClientSession.Character.Gold + requestData.Data.Item1.Amount <= maxGold)
            {
                if (requestData.Data.Item2.PickerType == VisualType.Npc)
                {
                    await requestData.ClientSession.SendPacket(
                        requestData.ClientSession.Character.GenerateIcon(1, requestData.Data.Item1.VNum));
                }

                requestData.ClientSession.Character.Gold += requestData.Data.Item1.Amount;
                await requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateSay(
                    $"{GameLanguage.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, requestData.ClientSession.Account.Language)}" +
                    $": {requestData.Data.Item1.ItemInstance!.Item!.Name[requestData.ClientSession.Account.Language]} x {requestData.Data.Item1.Amount}",
                    SayColorType.Green));
            }
            else
            {
                requestData.ClientSession.Character.Gold = maxGold;
                await requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
                        requestData.ClientSession.Account.Language),
                    Type = 0
                });
            }

            await requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateGold());
            requestData.ClientSession.Character.MapInstance!.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            await requestData.ClientSession.Character.MapInstance.SendPacket(
                requestData.ClientSession.Character.GenerateGet(requestData.Data.Item1.VisualId));
        }
    }
}