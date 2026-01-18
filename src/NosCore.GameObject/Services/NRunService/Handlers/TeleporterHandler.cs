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


using NosCore.Packets.Enumerations;
using NosCore.Shared.Helpers;
using System.Threading.Tasks;
using NosCore.GameObject.Services.MapChangeService;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Shared.Enumerations;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Ecs.Systems;

namespace NosCore.GameObject.Services.NRunService.Handlers
{
    public class TeleporterHandler(IMapChangeService mapChangeService, ICharacterPacketSystem characterPacketSystem) : INrunEventHandler
    {
        public bool Condition(NrunData item)
        {
            if (item.Packet.Runner != NrunRunnerType.Teleport || item.VisualType != VisualType.Npc)
            {
                return false;
            }
            var dialog = item.GetDialog() ?? 0;
            return ((dialog >= 439) && (dialog <= 441)) || (dialog == 11) ||
                    (dialog == 16) || (dialog == 9768);
        }

        public Task ExecuteAsync(RequestData<NrunData> requestData)
        {
            return requestData.Data.Packet.Type switch
            {
                1 => RemoveGoldAndTeleportAsync(requestData.ClientSession, 20, 1000, 7, 11, 90, 94),
                2 => RemoveGoldAndTeleportAsync(requestData.ClientSession, 145, 2000, 11, 15, 108, 112),
                _ => RemoveGoldAndTeleportAsync(requestData.ClientSession, 1, 0, 77, 82, 113, 119),
            };
        }

        private async Task RemoveGoldAndTeleportAsync(ClientSession clientSession, short mapId, long goldToPay, short x1, short x2,
            short y1, short y2)
        {
            var player = clientSession.Player;
            var currentGold = player.Gold;
            if (currentGold >= goldToPay)
            {
                player.SetGold(currentGold - goldToPay);
                await clientSession.SendPacketAsync(characterPacketSystem.GenerateGold(player)).ConfigureAwait(false);
                await mapChangeService.ChangeMapAsync(clientSession,
                    mapId, (short)RandomHelper.Instance.RandomNumber(x1, x2),
                    (short)RandomHelper.Instance.RandomNumber(y1, y2)).ConfigureAwait(false);
                return;
            }

            await clientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = player.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.NotEnoughGold
            }).ConfigureAwait(false);
        }
    }
}