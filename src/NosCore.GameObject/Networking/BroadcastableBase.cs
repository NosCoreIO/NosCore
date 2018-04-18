using NosCore.Core.Logger;
using NosCore.Core.Serializing;
using NosCore.Domain.Interaction;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NosCore.GameObject.Networking
{
    public abstract class BroadcastableBase
    {

        public ConcurrentDictionary<int, ClientSession> Sessions { get; set; } = new ConcurrentDictionary<int, ClientSession>();


        public void Broadcast(PacketDefinition packet)
        {
            Broadcast(null, packet);
        }

        public void Broadcast(PacketDefinition packet, int xRangeCoordinate, int yRangeCoordinate)
        {
            Broadcast(new BroadcastPacket(null, packet, ReceiverType.AllInRange, xCoordinate: xRangeCoordinate, yCoordinate: yRangeCoordinate));
        }

        public void Broadcast(ClientSession client, PacketDefinition packet, ReceiverType receiver = ReceiverType.All, string characterName = "", long characterId = -1)
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
                case ReceiverType.AllExceptMe:
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
                        if (sentPacket.Sender != null)
                        {
                            session.Value.SendPacket(sentPacket.Packet);
                        }
                    });
                    break;
            }
        }
    }
}
