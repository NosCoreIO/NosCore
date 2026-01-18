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
using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking;
using NosCore.Packets.ClientPackets.Drops;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System;
using System.Threading.Tasks;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Chats;

namespace NosCore.GameObject.Services.MapItemGenerationService.Handlers
{
    public class GoldDropEventHandler(IOptions<WorldConfiguration> worldConfiguration, ICharacterPacketSystem characterPacketSystem) : IGetMapItemEventHandler
    {
        public bool Condition(MapItemRef item)
        {
            return item.VNum == 1046;
        }

        public async Task ExecuteAsync(RequestData<Tuple<MapItemRef, GetPacket>> requestData)
        {
            var player = requestData.ClientSession.Player;
            var maxGold = worldConfiguration.Value.MaxGoldAmount;
            var currentGold = player.Gold;
            if (currentGold + requestData.Data.Item1.Amount <= maxGold)
            {
                if (requestData.Data.Item2.PickerType == VisualType.Npc)
                {
                    await requestData.ClientSession.SendPacketAsync(
                        characterPacketSystem.GenerateIcon(player, 1, requestData.Data.Item1.VNum)).ConfigureAwait(false);
                }

                player.SetGold(currentGold + requestData.Data.Item1.Amount);

#pragma warning disable NosCoreAnalyzers
                await requestData.ClientSession.SendPacketAsync(new Sayi2Packet
                {
                    VisualType = VisualType.Player,
                    VisualId = player.CharacterId,
                    Type = SayColorType.Green,
                    Message = Game18NConstString.ItemReceived,
                    ArgumentType = 9,
                    Game18NArguments = { requestData.Data.Item1.Amount, requestData.Data.Item1.ItemInstance!.Item.Name[requestData.ClientSession.Account.Language] }
                }).ConfigureAwait(false);
#pragma warning restore NosCoreAnalyzers
            }
            else
            {
                player.SetGold(maxGold);
                await requestData.ClientSession.SendPacketAsync(new MsgiPacket
                {
                    Type = MessageType.Default,
                    Message = Game18NConstString.MaxGoldReached
                }).ConfigureAwait(false);
            }

            await requestData.ClientSession.SendPacketAsync(characterPacketSystem.GenerateGold(player)).ConfigureAwait(false);
            player.MapInstance.MapItems.TryRemove(requestData.Data.Item1.VisualId, out _);
            await player.MapInstance.SendPacketAsync(
                characterPacketSystem.GenerateGet(player, requestData.Data.Item1.VisualId)).ConfigureAwait(false);
        }
    }
}