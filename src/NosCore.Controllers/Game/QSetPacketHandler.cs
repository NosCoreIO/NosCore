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

using System;
using System.Threading.Tasks;
using NosCore.Packets.ClientPackets.Quicklist;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Data.Dto;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Game
{
    public class QSetPacketHandler : PacketHandler<QsetPacket>, IWorldPacketHandler
    {
        private void SendQSet(ClientSession session, short q1, short q2, QSetType type, short data1, short data2)
        {
            session.SendPacket(new QsetClientPacket
            {
                OriginQuickList = q1,
                OriginQuickListSlot = q2,
                Data = new QsetClientSubPacket
                {
                    Type = type,
                    OriginQuickList = data1,
                    OriginQuickListSlot = data2,
                    Data = 0
                }
            });
        }

        public override Task Execute(QsetPacket qSetPacket, ClientSession session)
        {
            short data1 = 0, data2 = 0, q1 = qSetPacket.OriginQuickList, q2 = qSetPacket.OriginQuickListSlot;
            var type = qSetPacket.Type;
            var morph = session.Character.UseSp ? session.Character.Morph : (short) 0;
            if (qSetPacket.FirstData.HasValue)
            {
                data1 = qSetPacket.FirstData.Value;
            }

            if (qSetPacket.SecondData.HasValue)
            {
                data2 = qSetPacket.SecondData.Value;
            }

            switch (type)
            {
                case QSetType.Default:
                case QSetType.Set:
                    session.Character.QuicklistEntries.RemoveAll(
                        n => (n.Q1 == q1) && (n.Q2 == q2) && (n.Morph == morph));
                    session.Character.QuicklistEntries.Add(new QuicklistEntryDto
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = session.Character.CharacterId,
                        Type = type,
                        Q1 = q1,
                        Q2 = q2,
                        Slot = data1,
                        Pos = data2,
                        Morph = morph
                    });
                    SendQSet(session, q1, q2, type, data1, data2);
                    break;

                case QSetType.Move:
                    var qlFrom = session.Character.QuicklistEntries.Find(n =>
                        (n.Q1 == data1) && (n.Q2 == data2) && (n.Morph == morph));
                    if (qlFrom != null)
                    {
                        var qlTo = session.Character.QuicklistEntries.Find(n =>
                            (n.Q1 == q1) && (n.Q2 == q2) && (n.Morph == morph));

                        qlFrom.Q1 = q1;
                        qlFrom.Q2 = q2;

                        if (qlTo == null)
                        {
                            SendQSet(session, qlFrom.Q1, qlFrom.Q2, qlFrom.Type, qlFrom.Slot, qlFrom.Pos);
                            SendQSet(session, data1, data2, QSetType.Reset, 7, -1);
                        }
                        else
                        {
                            SendQSet(session, qlFrom.Q1, qlFrom.Q2, qlFrom.Type, qlFrom.Slot, qlFrom.Pos);
                            qlTo.Q1 = data1;
                            qlTo.Q2 = data2;
                            SendQSet(session, qlTo.Q1, qlTo.Q2, qlTo.Type, qlTo.Slot, qlTo.Pos);
                        }
                    }

                    break;

                case QSetType.Remove:
                    session.Character.QuicklistEntries.RemoveAll(
                        n => (n.Q1 == q1) && (n.Q2 == q2) && (n.Morph == morph));
                    SendQSet(session, q1, q2, QSetType.Reset, 7, -1);
                    break;

                default:
                    return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
    }
}