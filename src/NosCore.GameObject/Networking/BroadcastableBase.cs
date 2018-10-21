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
    public class BroadcastableBase //TODO remove
    {
        protected BroadcastableBase()
        {
            LastUnregister = DateTime.Now.AddMinutes(-1);
            Sessions = new ConcurrentDictionary<long, ClientSession>();
        }

        public ConcurrentDictionary<long, ClientSession> Sessions { get; set; }

        protected DateTime LastUnregister { get; private set; }

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
                        Sessions.Where(s => s.Value.HasSelectedCharacter
                            && s.Value.Character.CharacterId != sentPacket.Sender.Character.CharacterId
                            && !s.Value.Character.IsRelatedToCharacter(sentPacket.Sender.Character.CharacterId,
                                CharacterRelationType.Blocked)),
                        session => session.Value.SendPacket(sentPacket.Packet));
                    break;
                case ReceiverType.AllExceptMe:
                    Parallel.ForEach(
                        Sessions.Values.Where(s =>
                            s.HasSelectedCharacter
                            && s.Character.CharacterId != sentPacket.Sender.Character.CharacterId),
                        session => session.SendPacket(sentPacket.Packet));
                    break;
                case ReceiverType.Group:
                    Parallel.ForEach(
                        sentPacket.Sender.Character.Group.Values.Where(s => s.Item2.VisualType == VisualType.Player),
                        entity =>
                        {
                            var session =
                                Sessions.Values.FirstOrDefault(s => s.Character.CharacterId == entity.Item2.VisualId);

                            session?.SendPacket(sentPacket.Packet);
                        });
                    break;
                case ReceiverType.AllExceptGroup:
                case ReceiverType.AllNoEmoBlocked:
                case ReceiverType.AllNoHeroBlocked:
                case ReceiverType.AllInRange:
                case ReceiverType.All:
                    Parallel.ForEach(Sessions.Where(s => s.Value.HasSelectedCharacter),
                        session => session.Value.SendPacket(sentPacket.Packet));
                    break;
                default:
                    return;
            }
        }
    }
}