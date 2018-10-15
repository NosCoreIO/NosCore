using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Serializing;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
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

        public void RegisterSession(ClientSession clientSession)
        {
            Sessions.TryAdd(clientSession.SessionId, clientSession);
        }

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
                clientSession.Character.MapInstance?.Broadcast(clientSession.Character.GenerateOut());

                clientSession.Character.Save();
            }

            Sessions.TryRemove(clientSession.SessionId, out _);
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
                        Sessions.Where(s => s.Value.HasSelectedCharacter && s.Value.Character.CharacterId != sentPacket.Sender.Character.CharacterId
                            && !s.Value.Character.IsRelatedToCharacter(sentPacket.Sender.Character.CharacterId, CharacterRelationType.Blocked)),
                        session => session.Value.SendPacket(sentPacket.Packet));
                    break;
                case ReceiverType.AllExceptMe:
                    Parallel.ForEach(
                        Sessions.Values.Where(s => s.HasSelectedCharacter && s.Character.CharacterId != sentPacket.Sender.Character.CharacterId),
                        session => session.SendPacket(sentPacket.Packet));
                    break;
                case ReceiverType.Group:
                    Parallel.ForEach(sentPacket.Sender.Character.Group.Values.Where(s => s.Item2.VisualType == VisualType.Player), entity =>
                    {
                        var session = Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == entity.Item2.VisualId);

                        session?.SendPacket(sentPacket.Packet);
                    });
                    break;
                case ReceiverType.AllExceptGroup:
                case ReceiverType.AllNoEmoBlocked:
                case ReceiverType.AllNoHeroBlocked:
                case ReceiverType.AllInRange:
                case ReceiverType.All:
                    Parallel.ForEach(Sessions.Where(s => s.Value.HasSelectedCharacter), session => session.Value.SendPacket(sentPacket.Packet));
                    break;
                default:
                    return;
            }
        }
    }
}