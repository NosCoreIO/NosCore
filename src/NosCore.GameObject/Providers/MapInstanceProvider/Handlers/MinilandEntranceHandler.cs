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

using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using ChickenAPI.Packets.ClientPackets.UI;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.Data.Enumerations.Map;
using NosCore.Core.I18N;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.Data.Enumerations.I18N;
using ChickenAPI.Packets.Enumerations;

namespace NosCore.GameObject.Providers.GuriProvider.Handlers
{
    public class MinilandEntranceHandler : IEventHandler<Map.Map, MapInstance>, IMapInstanceEventHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public MapInstanceEventType MapInstanceEventType => MapInstanceEventType.Entrance;

        public MinilandEntranceHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public bool Condition(Map.Map map) => map.MapId == 20001;

        public void Execute(RequestData<MapInstance> requestData)
        {
            var miniland = _minilandProvider.GetMinilandInfoFromMapInstanceId(requestData.Data.MapInstanceId);
            if (miniland == null)
            {
                return;
            }

            if (miniland.Owner != requestData.ClientSession.Character.CharacterId)
            {
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = miniland.MinilandMessage.Replace(' ', '^')
                });

                requestData.ClientSession.SendPacket(miniland.GenerateMlinfobr());
                //TODO add entrance
            }
            else
            {
                requestData.ClientSession.SendPacket(miniland.GenerateMlinfo());
            }
            //TODO add pets
            //TODO add entrance counts
            requestData.ClientSession.SendPacket(
                requestData.ClientSession.Character.GenerateSay(
                    string.Format(Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_VISITOR, requestData.ClientSession.Account.Language), 0, 0), SayColorType.Yellow)
                );
        }
    }
}