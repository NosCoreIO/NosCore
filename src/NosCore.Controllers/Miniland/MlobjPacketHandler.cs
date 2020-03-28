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

using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;

namespace NosCore.PacketHandlers.Miniland
{
    public class MlEditPacketHandler : PacketHandler<MLEditPacket>, IWorldPacketHandler
    {
        private readonly IMinilandProvider _minilandProvider;

        public MlEditPacketHandler(IMinilandProvider minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public override Task Execute(MLEditPacket mlEditPacket, ClientSession clientSession)
        {
            var miniland = _minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            switch (mlEditPacket.Type)
            {
                case 1:
                    clientSession.SendPacket(new MlintroPacket {Intro = mlEditPacket.MinilandInfo.Replace(' ', '^')});
                    miniland.MinilandMessage = mlEditPacket.MinilandInfo;
                    clientSession.SendPacket(new InfoPacket
                    {
                        Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_INFO_CHANGED,
                            clientSession.Account.Language)
                    });
                    break;

                case 2:
                    switch (mlEditPacket.Parameter)
                    {
                        case MinilandState.Private:
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_PRIVATE,
                                    clientSession.Account.Language)
                            });
                            _minilandProvider.SetState(clientSession.Character.CharacterId, MinilandState.Private);
                            break;

                        case MinilandState.Lock:
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_LOCK,
                                    clientSession.Account.Language)
                            });
                            _minilandProvider.SetState(clientSession.Character.CharacterId, MinilandState.Lock);
                            break;

                        case MinilandState.Open:
                            clientSession.SendPacket(new MsgPacket
                            {
                                Message = Language.Instance.GetMessageFromKey(LanguageKey.MINILAND_PUBLIC,
                                    clientSession.Account.Language)
                            });
                            _minilandProvider.SetState(clientSession.Character.CharacterId, MinilandState.Open);
                            break;

                        default:
                            return Task.CompletedTask;
                    }

                    break;
            }
            return Task.CompletedTask;
        }
    }
}