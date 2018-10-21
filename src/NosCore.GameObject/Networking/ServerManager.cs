//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NosCore.Core;
using NosCore.Core.Networking;
using NosCore.Data.WebApi;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public sealed class ServerManager : BroadcastableBase
    {
        private static ServerManager _instance;
        private long _lastGroupId = 1;

        public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

        public ConcurrentDictionary<long, Group> Groups { get; set; } = new ConcurrentDictionary<long, Group>();

        private void LaunchEvents()
        {
            Observable.Interval(TimeSpan.FromMinutes(5)).Subscribe(x => SaveAll());
        }

        public long GetNextGroupId()
        {
            return ++_lastGroupId;
        }

        public void SaveAll()
        {
            try
            {
                Logger.Log.Info(LogLanguage.Instance.GetMessageFromKey(LanguageKey.SAVING_ALL));
                Parallel.ForEach(Sessions.Values.Where(s => s.Character != null), session => session.Character.Save());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void BroadcastPacket(PostedPacket packet) => BroadcastPacket(packet, null);
        public void BroadcastPacket(PostedPacket postedPacket, int? channelId)
        {
            if (channelId == null)
            {
                foreach (var channel in WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels"))
                {
                    WebApiAccess.Instance.Post<PostedPacket>("api/packet", postedPacket, channel.WebApi);
                }
            }
            else
            {
                var channel = WebApiAccess.Instance.Get<List<WorldServerInfo>>("api/channels", id: channelId.Value)
                    .FirstOrDefault();
                if (channel != null)
                {
                    WebApiAccess.Instance.Post<PostedPacket>("api/packet", postedPacket, channel.WebApi);
                }
            }
        }

        public void BroadcastPackets(List<PostedPacket> packets) => BroadcastPackets(packets, null);
        public void BroadcastPackets(List<PostedPacket> packets, int? channelId)
        {
            foreach (var packet in packets)
            {
                BroadcastPacket(packet, channelId);
            }
        }
    }
}