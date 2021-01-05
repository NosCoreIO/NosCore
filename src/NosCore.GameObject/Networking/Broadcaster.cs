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

using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels.Groups;
using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Packets.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Networking
{
    public class Broadcaster : IBroadcastable
    {
        public short MaxPacketsBuffer { get; } = 250;
        private static Broadcaster? _instance;

        private Broadcaster()
        {
            ExecutionEnvironment.TryGetCurrentExecutor(out var executor);
            Sessions = new DefaultChannelGroup(executor);
        }

        private ConcurrentDictionary<long, ClientSession.ClientSession> ClientSessions { get; } =
            new ConcurrentDictionary<long, ClientSession.ClientSession>();

        public static Broadcaster Instance => _instance ??= new Broadcaster();

        public IChannelGroup Sessions { get; set; }

        public ConcurrentQueue<IPacket> LastPackets { get; } = new ConcurrentQueue<IPacket>();

        public void UnregisterSession(ClientSession.ClientSession clientSession)
        {
            ClientSessions.TryRemove(clientSession.SessionId, out _);

            if (clientSession.Channel?.Id != null)
            {
                Sessions.Remove(clientSession.Channel);
            }
        }

        public void RegisterSession(ClientSession.ClientSession clientSession)
        {
            if (clientSession.Channel?.Id != null)
            {
                Sessions.Add(clientSession.Channel);
            }

            ClientSessions.TryAdd(clientSession.SessionId, clientSession);
        }

        public IEnumerable<ICharacterEntity> GetCharacters()
        {
            return GetCharacters(null);
        }

        public IEnumerable<ICharacterEntity> GetCharacters(Func<ICharacterEntity, bool>? func)
        {
            return func == null ? ClientSessions.Values.Where(s => s.Character != null).Select(s => s.Character!)
                : ClientSessions.Values.Where(s => s.Character != null).Select(c => c.Character!).Where(func);
        }

        public ICharacterEntity? GetCharacter(Func<ICharacterEntity, bool>? func)
        {
            return func == null ? ClientSessions.Values.FirstOrDefault(s => s.Character != null)?.Character
                : ClientSessions.Values.Where(s => s.Character != null).Select(c => c.Character!).FirstOrDefault(func);
        }

        public static void Reset()
        {
            _instance = null;
        }

        public List<ConnectedAccount> ConnectedAccounts()
        {
            return ClientSessions.Values.Select(s =>
                new ConnectedAccount
                {
                    Name = s.Account.Name,
                    Language = s.Account.Language,
                    ConnectedCharacter = s.Character == null ? null : new Data.WebApi.Character
                    {
                        Name = s.Character.Name, Id = s.Character.CharacterId,
                        FriendRequestBlocked = s.Character.FriendRequestBlocked
                    }
                }).ToList();
        }
    }
}