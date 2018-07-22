using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public abstract class BroadcastableBase : IDisposable
    {
        private bool _disposed;
        public virtual void Dispose()
        {
            if (!_disposed)
            {
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        public void RegisterSession(ClientSession session)
        {
            if (!session.HasSelectedCharacter)
            {
                return;
            }
            Sessions[session.Character.CharacterId] = session;
            if (session.HasCurrentMapInstance)
            {
                session.Character.MapInstance.IsSleeping = false;
            }
        }


        public void UnregisterSession(ClientSession session)
        {
            if (!session.HasSelectedCharacter)
            {
                return;
            }
            // Remove client from online clients list
            if (!Sessions.TryRemove(session.Character.CharacterId, out _))
            {
                return;
            }
            if (session.HasCurrentMapInstance && Sessions.Count == 0)
            {
                session.Character.MapInstance.IsSleeping = true;
            }

            if (session.Character != null)
            {
                if (session.Character.Hp < 1)
                {
                    session.Character.Hp = 1;
                }

                session.Character.Save();

                _sessions.TryRemove(session.Character.CharacterId, out _);
            }
            LastUnregister = DateTime.Now;
        }

        public ConcurrentDictionary<long, ClientSession> Sessions { get; set; }

        protected DateTime LastUnregister { get; private set; }


        protected BroadcastableBase()
        {
            LastUnregister = DateTime.Now.AddMinutes(-1);
            Sessions = new ConcurrentDictionary<long, ClientSession>();
        }

        public void Broadcast(PacketDefinition packet)
        {
            Broadcast(null, packet);
        }

        public void Broadcast(PacketDefinition packet, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(new BroadcastPacket(null, packet, ReceiverType.AllInRange, xCoordinate: xRangeCoordinate,
                yCoordinate: yRangeCoordinate));
        }

        public void Broadcast(ClientSession client, PacketDefinition packet, ReceiverType receiver = ReceiverType.All,
            string characterName = "", long characterId = -1)
        {
            try
            {
                SpreadBroadcastpacket(new BroadcastPacket(client, packet, receiver, characterName, characterId));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Broadcast(BroadcastPacket packet)
        {
            try
            {
                SpreadBroadcastpacket(packet);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void SpreadBroadcastpacket(BroadcastPacket sentPacket)
        {
            if (Sessions == null || sentPacket?.Packet == null)
            {
                return;
            }

            switch (sentPacket.Receiver)
            {
                case ReceiverType.AllExceptMeAndBlacklisted:
                    Parallel.ForEach(
                        Sessions.Where(s => s.Value.Character.CharacterId != sentPacket.Sender.Character.CharacterId && !s.Value.Character.IsBlockedByCharacter(sentPacket.Sender.Character.CharacterId)),
                        session =>
                        {
                            if (!session.Value.HasSelectedCharacter)
                            {
                                return;
                            }

                            session.Value.SendPacket(sentPacket.Packet);
                        });
                    break;
                case ReceiverType.AllExceptMe:
                    Parallel.ForEach(
                        Sessions.Values.Where(s => s.Character.CharacterId != sentPacket.Sender.Character.CharacterId),
                        session =>
                        {
                            if (!session.HasSelectedCharacter)
                            {
                                return;
                            }

                            session.SendPacket(sentPacket.Packet);
                        });
                    break;
                case ReceiverType.AllExceptGroup:
                case ReceiverType.AllNoEmoBlocked:
                case ReceiverType.AllNoHeroBlocked:
                case ReceiverType.Group:
                case ReceiverType.AllInRange:
                case ReceiverType.All:
                    Parallel.ForEach(Sessions, session =>
                    {
                        if (!session.Value.HasSelectedCharacter)
                        {
                            return;
                        }

                        session.Value.SendPacket(sentPacket.Packet);
                    });
                    break;
            }
        }
    }
}