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

using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Data.CommandPackets;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.StatHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.GameObject.HubClients.ChannelHubClient;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Command
{
    public class SetLevelCommandPacketHandler : PacketHandler<SetLevelCommandPacket>, IWorldPacketHandler
    {
        private readonly IChannelHubClient _channelHubClient;
        private readonly IStatHttpClient _statHttpClient;

        public SetLevelCommandPacketHandler(IChannelHubClient channelHubClient, IStatHttpClient statHttpClient)
        {
            _channelHubClient = channelHubClient;
            _statHttpClient = statHttpClient;
        }

        public override async Task ExecuteAsync(SetLevelCommandPacket levelPacket, ClientSession session)
        {
            if (string.IsNullOrEmpty(levelPacket.Name) || (levelPacket.Name == session.Character.Name))
            {
                await session.Character.SetLevelAsync(levelPacket.Level).ConfigureAwait(false);
                return;
            }

            var data = new StatData
            {
                ActionType = UpdateStatActionType.UpdateLevel,
                Character = new Character { Name = levelPacket.Name },
                Data = levelPacket.Level
            };

            var target = await
                _channelHubClient.GetCharacterAsync(null, levelPacket.Name).ConfigureAwait(false);

            if (target == default)
            {
                return;
            }

            if (target == null)
            {
                await session.SendPacketAsync(new InfoPacket
                {
                    Message = GameLanguage.Instance.GetMessageFromKey(LanguageKey.CANT_FIND_CHARACTER,
                        session.Account.Language)
                }).ConfigureAwait(false);
                return;
            }

            await _statHttpClient.ChangeStatAsync(data, target.ChannelId!).ConfigureAwait(false);
        }
    }
}