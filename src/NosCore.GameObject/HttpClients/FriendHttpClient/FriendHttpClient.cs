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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NosCore.Packets.Enumerations;
using NosCore.Core;
using NosCore.Core.HttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;

namespace NosCore.GameObject.HttpClients.FriendHttpClient
{
    public class FriendHttpClient : MasterServerHttpClient, IFriendHttpClient
    {
        public FriendHttpClient(IHttpClientFactory httpClientFactory, Channel channel,
            IChannelHttpClient channelHttpClient)
            : base(httpClientFactory, channel, channelHttpClient)
        {
            ApiUrl = "api/friend";
            RequireConnection = true;
        }

        public Task<LanguageKey> AddFriend(FriendShipRequest friendShipRequest)
        {
            return Post<LanguageKey>(friendShipRequest);
        }

        public async Task<List<CharacterRelationStatus>> GetListFriends(long visualEntityVisualId)
        {
            return (await Get<List<CharacterRelationStatus>>(visualEntityVisualId)!)
                .Where(w => w.RelationType != CharacterRelationType.Blocked)
                .ToList();
        }

        public Task DeleteFriend(Guid characterRelationId)
        {
            return Delete(characterRelationId);
        }
    }
}