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

using NosCore.GameObject.Ecs.Systems;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using System.Threading.Tasks;

//TODO stop using obsolete
#pragma warning disable 618

namespace NosCore.GameObject.Services.MapInstanceGenerationService.Handlers
{
    public class MinilandEntranceHandler(IMinilandService minilandProvider, ICharacterPacketSystem characterPacketSystem) : IMapInstanceEntranceEventHandler
    {
        public bool Condition(MapInstance condition) => minilandProvider.GetMinilandFromMapInstanceId(condition.MapInstanceId) != null;

        public async Task ExecuteAsync(RequestData<MapInstance> requestData)
        {
            var miniland = minilandProvider.GetMinilandFromMapInstanceId(requestData.Data.MapInstanceId)!;
            if (miniland.OwnerId != requestData.ClientSession.Player.CharacterId)
            {
                await requestData.ClientSession.SendPacketAsync(new MsgPacket
                {
                    Message = miniland.MinilandMessage!.Replace(' ', '^')
                }).ConfigureAwait(false);

                miniland.DailyVisitCount++;
                miniland.VisitCount++;
                await requestData.ClientSession.SendPacketAsync(miniland.GenerateMlinfobr()).ConfigureAwait(false);
            }
            else
            {
                await requestData.ClientSession.SendPacketAsync(miniland.GenerateMlinfo()).ConfigureAwait(false);
                await requestData.ClientSession.SendPacketAsync(characterPacketSystem.GenerateMlobjlst(requestData.ClientSession.Player)).ConfigureAwait(false);
            }

            //TODO add pets

            await requestData.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = requestData.ClientSession.Player.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.TotalVisitors,
                ArgumentType = 4,
                Game18NArguments = { miniland.VisitCount }
            }).ConfigureAwait(false);

            await requestData.ClientSession.SendPacketAsync(new SayiPacket
            {
                VisualType = VisualType.Player,
                VisualId = requestData.ClientSession.Player.CharacterId,
                Type = SayColorType.Yellow,
                Message = Game18NConstString.TodayVisitors,
                ArgumentType = 4,
                Game18NArguments = { miniland.DailyVisitCount }
            }).ConfigureAwait(false);
        }
    }
}