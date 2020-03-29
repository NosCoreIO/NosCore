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
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Map;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.GameObject.Providers.MapInstanceProvider.Handlers
{
    public class MinilandEntranceHandler : IMapInstanceEventHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public MinilandEntranceHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public MapInstanceEventType MapInstanceEventType => MapInstanceEventType.Entrance;

        public void Execute(RequestData<MapInstance> requestData)
        {
            var miniland = _minilandProvider.GetMinilandFromMapInstanceId(requestData.Data.MapInstanceId);
            if (miniland.CharacterEntity!.VisualId != requestData.ClientSession.Character.CharacterId)
            {
                requestData.ClientSession.SendPacket(new MsgPacket
                {
                    Message = miniland.MinilandMessage!.Replace(' ', '^')
                });

                miniland.DailyVisitCount++;
                miniland.VisitCount++;
                requestData.ClientSession.SendPacket(miniland.GenerateMlinfobr());
            }
            else
            {
                requestData.ClientSession.SendPacket(miniland.GenerateMlinfo());
                requestData.ClientSession.SendPacket(requestData.ClientSession.Character.GenerateMlobjlst());
            }

            //TODO add pets
            requestData.ClientSession.SendPacket(
                requestData.ClientSession.Character.GenerateSay(
                    string.Format(
                        Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_VISITOR,
                            requestData.ClientSession.Account.Language), miniland.VisitCount, miniland.DailyVisitCount),
                    SayColorType.Yellow)
            );
        }
    }
}