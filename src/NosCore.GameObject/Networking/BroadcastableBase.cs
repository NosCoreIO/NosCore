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
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels.Groups;
using NosCore.GameObject.ComponentEntities.Extensions;

namespace NosCore.GameObject.Networking
{
    public class BroadcastableBase
    {
        public BroadcastableBase()
        {
            ExecutionEnvironment.TryGetCurrentExecutor(out var executor);
            Sessions = new DefaultChannelGroup(executor);
        }

        public IChannelGroup Sessions { get; set; }

        public void UnregisterSession(ClientSession clientSession)
        {
            if (clientSession.Character != null)
            {
                if (clientSession.Character.Hp < 1)
                {
                    clientSession.Character.Hp = 1;
                }

                clientSession.Character.SendRelationStatus(false);
                clientSession.Character.LeaveGroup();
                clientSession.Character.MapInstance?.Sessions.SendPacket(clientSession.Character.GenerateOut());

                clientSession.Character.Save();
            }
            ServerManager.Instance.ClientSessions.TryRemove(clientSession.SessionId, out _);

            if (clientSession.Channel != null)
            {
                Sessions.Remove(clientSession.Channel);
            }
            LastUnregister = DateTime.Now;
        }

        public void RegisterSession(ClientSession clientSession)
        {
            if (clientSession.Channel != null)
            {
                Sessions.Add(clientSession.Channel);
            }
            ServerManager.Instance.ClientSessions.TryAdd(clientSession.SessionId, clientSession);
        }

        public DateTime LastUnregister { get; set; }
    }
}