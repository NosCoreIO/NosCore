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

using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MinilandService;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Miniland;
using NosCore.Packets.ServerPackets.UI;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Miniland
{
    public class MlEditPacketHandler : PacketHandler<MLEditPacket>, IWorldPacketHandler
    {
        private readonly IMinilandService _minilandProvider;

        public MlEditPacketHandler(IMinilandService minilandProvider)
        {
            _minilandProvider = minilandProvider;
        }

        public override async Task ExecuteAsync(MLEditPacket mlEditPacket, ClientSession clientSession)
        {
            var miniland = _minilandProvider.GetMiniland(clientSession.Character.CharacterId);
            switch (mlEditPacket.Type)
            {
                case 1:
                    await clientSession.SendPacketAsync(new MlintroPacket { Intro = mlEditPacket.MinilandInfo!.Replace(' ', '^') }).ConfigureAwait(false);
                    miniland.MinilandMessage = mlEditPacket.MinilandInfo;
                    await clientSession.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.MinilandChanged
                    }).ConfigureAwait(false);
                    break;

                case 2:
                    switch (mlEditPacket.Parameter)
                    {
                        case MinilandState.Private:
                            await clientSession.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.MinilandPrivate
                            }).ConfigureAwait(false);
                            await _minilandProvider.SetStateAsync(clientSession.Character.CharacterId, MinilandState.Private).ConfigureAwait(false);
                            break;

                        case MinilandState.Lock:
                            await clientSession.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.MinilandLocked
                            }).ConfigureAwait(false);
                            await _minilandProvider.SetStateAsync(clientSession.Character.CharacterId, MinilandState.Lock).ConfigureAwait(false);
                            break;

                        case MinilandState.Open:
                            await clientSession.SendPacketAsync(new MsgiPacket
                            {
                                Type = MessageType.Default,
                                Message = Game18NConstString.MinilandPublic
                            }).ConfigureAwait(false);
                            await _minilandProvider.SetStateAsync(clientSession.Character.CharacterId, MinilandState.Open).ConfigureAwait(false);
                            break;

                        default:
                            return;
                    }

                    break;
            }
        }
    }
}