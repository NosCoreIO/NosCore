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

using System.Collections.Generic;
using NosCore.Configuration;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;

namespace NosCore.Core.Networking
{
    public interface IWebApiAccess
    {
        string Token { get; set; }

        T Delete<T>(WebApiRoute route, ServerConfiguration webApi);
        T Delete<T>(WebApiRoute route, object id);
        T Delete<T>(WebApiRoute route);
        T Delete<T>(WebApiRoute route, ServerConfiguration webApi, object id);
        T Get<T>(WebApiRoute route, object id);
        T Get<T>(WebApiRoute route, ServerConfiguration webApi);
        T Get<T>(WebApiRoute route);
        T Get<T>(WebApiRoute route, ServerConfiguration webApi, object id);
        T Post<T>(WebApiRoute route, object data);
        T Post<T>(WebApiRoute route, object data, ServerConfiguration webApi);
        T Put<T>(WebApiRoute route, object data);
        T Put<T>(WebApiRoute route, object data, ServerConfiguration webApi);
        T Patch<T>(WebApiRoute route, object id, ServerConfiguration webApi);
        T Patch<T>(WebApiRoute route, object id, object data);
        T Patch<T>(WebApiRoute route, object id, object data, ServerConfiguration webApi);
        void BroadcastPacket(PostedPacket packet, int channelId);
        void BroadcastPacket(PostedPacket packet);
        void BroadcastPackets(List<PostedPacket> packets);
        void BroadcastPackets(List<PostedPacket> packets, int channelId);
        void RegisterBaseAdress(Channel channel);
        (ServerConfiguration, ConnectedAccount) GetCharacter(long? characterId, string characterName);
    }
}