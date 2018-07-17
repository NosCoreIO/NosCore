using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NosCore.Core.Serializing;
using NosCore.Shared.Enumerations.Interaction;
using NosCore.Shared.I18N;

namespace NosCore.GameObject.Networking
{
    public abstract class BroadcastableBase
    {
        public ConcurrentDictionary<int, ClientSession> Sessions { get; set; } =
            new ConcurrentDictionary<int, ClientSession>();

        public void Broadcast(PacketDefinition packet)
        {
            Broadcast(null, packet);
        }

        public void Broadcast(string packet)
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

        public void Broadcast(ClientSession client, string packet, ReceiverType receiver = ReceiverType.All,
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
            if (Sessions == null || sentPacket?.Packet == null && sentPacket?.PacketString == null)
            {
                return;
            }

            switch (sentPacket.Receiver)
            {
                case ReceiverType.AllExceptMe:
                    Parallel.ForEach(Sessions.Where(s => s.Value.Character.CharacterId != sentPacket.Sender.Character.CharacterId), session =>
                    {
                        if (!session.Value.HasSelectedCharacter)
                        {
                            return;
                        }

                        if (sentPacket.Packet == null)
                        {
                            session.Value.SendPacket(sentPacket.PacketString);
                        }
                        else
                        {
                            session.Value.SendPacket(sentPacket.Packet);
                        }
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

                        if (sentPacket.Packet == null)
                        {
                            session.Value.SendPacket(sentPacket.PacketString);
                        }
                        else
                        {
                            session.Value.SendPacket(sentPacket.Packet);
                        }
                    });
                    break;
            }
        }
    }
}