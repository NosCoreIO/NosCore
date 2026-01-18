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

using NosCore.Data.Dto;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.ClientPackets.Quicklist;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using System;
using System.Threading.Tasks;
using NosCore.GameObject.Infastructure;

namespace NosCore.PacketHandlers.Game
{
    public class QSetPacketHandler : PacketHandler<QsetPacket>, IWorldPacketHandler
    {
        private async Task SendQSetAsync(ClientSession session, short q1, short q2, QSetType type, short data1, short data2)
        {
            await session.SendPacketAsync(new QsetClientPacket
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
            }).ConfigureAwait(false);
        }

        public override async Task ExecuteAsync(QsetPacket qSetPacket, ClientSession session)
        {
            short data1 = 0, data2 = 0, quickListIndex = qSetPacket.OriginQuickList, q2 = qSetPacket.OriginQuickListSlot;
            var type = qSetPacket.Type;
            var morph = session.Character.UseSp ? session.Character.Morph : (short)0;
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
                        n => (n.QuickListIndex == quickListIndex) && (n.Slot == q2) && (n.Morph == morph));
                    session.Character.QuicklistEntries.Add(new QuicklistEntryDto
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = session.Character.CharacterId,
                        Type = (short)type,
                        QuickListIndex = quickListIndex,
                        Slot = q2,
                        IconType = data1,
                        IconVNum = data2,
                        Morph = morph
                    });
                    await SendQSetAsync(session, quickListIndex, q2, type, data1, data2).ConfigureAwait(false);
                    break;

                case QSetType.Move:
                    var qlFrom = session.Character.QuicklistEntries.Find(n =>
                        (n.QuickListIndex == data1) && (n.Slot == data2) && (n.Morph == morph));
                    if (qlFrom != null)
                    {
                        var qlTo = session.Character.QuicklistEntries.Find(n =>
                            (n.QuickListIndex == quickListIndex) && (n.Slot == q2) && (n.Morph == morph));

                        qlFrom.QuickListIndex = quickListIndex;
                        qlFrom.Slot = q2;

                        if (qlTo == null)
                        {
                            await SendQSetAsync(session, qlFrom.QuickListIndex, qlFrom.Slot, (QSetType)qlFrom.Type, qlFrom.IconType, qlFrom.IconVNum).ConfigureAwait(false);
                            await SendQSetAsync(session, data1, data2, QSetType.Reset, 7, -1).ConfigureAwait(false);
                        }
                        else
                        {
                            await SendQSetAsync(session, qlFrom.QuickListIndex, qlFrom.Slot, (QSetType)qlFrom.Type, qlFrom.IconType, qlFrom.IconVNum).ConfigureAwait(false);
                            qlTo.QuickListIndex = data1;
                            qlTo.Slot = data2;
                            await SendQSetAsync(session, qlTo.QuickListIndex, qlTo.Slot, (QSetType)qlTo.Type, qlTo.IconType, qlTo.IconVNum).ConfigureAwait(false);
                        }
                    }

                    break;

                case QSetType.Remove:
                    session.Character.QuicklistEntries.RemoveAll(
                        n => (n.QuickListIndex == quickListIndex) && (n.Slot == q2) && (n.Morph == morph));
                    await SendQSetAsync(session, quickListIndex, q2, QSetType.Reset, 7, -1).ConfigureAwait(false);
                    break;

                default:
                    return;
            }
        }
    }
}