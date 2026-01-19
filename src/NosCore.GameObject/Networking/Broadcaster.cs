//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.Networking.SessionGroup;
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
        private static ISessionGroupFactory? _sessionGroupFactory;

        private Broadcaster(ISessionGroupFactory sessionGroupFactory)
        {
            Sessions = sessionGroupFactory.Create();
        }

        private ConcurrentDictionary<long, ClientSession.ClientSession> ClientSessions { get; } = new();

        public static void Initialize(ISessionGroupFactory sessionGroupFactory)
        {
            _sessionGroupFactory = sessionGroupFactory;
        }

        public static Broadcaster Instance => _instance ??= new Broadcaster(_sessionGroupFactory
            ?? throw new InvalidOperationException("Broadcaster.Initialize must be called before accessing Instance"));

        public ISessionGroup Sessions { get; set; }

        public ConcurrentQueue<IPacket> LastPackets { get; } = new();

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
            var selection = ClientSessions.Values.Where(s => s.Character != null!).Select(s => s.Character);
            return func == null ? selection : selection.Where(func);
        }

        public ICharacterEntity? GetCharacter(Func<ICharacterEntity, bool>? func)
        {
            var selection = ClientSessions.Values.Where(s => s.Character != null!).Select(c => c.Character);
            return func == null ? selection.FirstOrDefault() : selection.FirstOrDefault(func);
        }

        public static void Reset()
        {
            _instance = null;
        }

        public List<Subscriber> ConnectedAccounts()
        {
            return ClientSessions.Values.Select(s =>
                new Subscriber
                {
                    Name = s.Account.Name,
                    Language = s.Account.Language,
                    ConnectedCharacter = s.Character == null! ? null : new Data.WebApi.Character
                    {
                        Name = s.Character.Name, Id = s.Character.CharacterId,
                        FriendRequestBlocked = s.Character.FriendRequestBlocked
                    }
                }).ToList();
        }
    }
}
