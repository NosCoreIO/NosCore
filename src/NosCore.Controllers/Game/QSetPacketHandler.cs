using ChickenAPI.Packets.ClientPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ClientPackets.Quicklist;
using ChickenAPI.Packets.Enumerations;
using ChickenAPI.Packets.Interfaces;
using ChickenAPI.Packets.ServerPackets.CharacterSelectionScreen;
using ChickenAPI.Packets.ServerPackets.UI;
using NosCore.Configuration;
using NosCore.Core.HttpClients.ChannelHttpClient;
using NosCore.Core.HttpClients.ConnectedAccountHttpClient;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.HttpClients.PacketHttpClient;
using NosCore.GameObject.Networking.ClientSession;

namespace NosCore.PacketHandlers.Game
{
    public class QSetPacketHandler : PacketHandler<QsetPacket>, IWorldPacketHandler
    {
        public override void Execute(QsetPacket qSetPacket, ClientSession session)
        {
            short data1 = 0, data2 = 0, type = qSetPacket.Type, q1 = qSetPacket.Q1, q2 = qSetPacket.Q2;
            if (qSetPacket.Data1.HasValue)
            {
                data1 = qSetPacket.Data1.Value;
            }
            if (qSetPacket.Data2.HasValue)
            {
                data2 = qSetPacket.Data2.Value;
            }
            switch (type)
            {
                //case 0:
                //case 1:
                //    // client says qset 0 1 3 2 6 answer -> qset 1 3 0.2.6.0
                //    session.Character.QuicklistEntries.RemoveAll(n => n.Q1 == q1 && n.Q2 == q2 && (Session.Character.UseSp ? n.Morph == Session.Character.Morph : n.Morph == 0));

                //    session.Character.QuicklistEntries.Add(new QuicklistEntryDTO
                //    {
                //        CharacterId = session.Character.CharacterId,
                //        Type = type,
                //        Q1 = q1,
                //        Q2 = q2,
                //        Slot = data1,
                //        Pos = data2,
                //        Morph = session.Character.UseSp ? session.Character.Morph : 0
                //    });

                //    session.SendPacket($"qset {q1} {q2} {type}.{data1}.{data2}.0");
                //    break;

                //case 2:

                //    // DragDrop / Reorder qset type to1 to2 from1 from2 vars -> q1 q2 data1 data2
                //    var qlFrom = session.Character.QuicklistEntries.SingleOrDefault(n => n.Q1 == data1 && n.Q2 == data2 && (Session.Character.UseSp ? n.Morph == Session.Character.Morph : n.Morph == 0));

                //    if (qlFrom != null)
                //    {
                //        var qlTo = session.Character.QuicklistEntries.SingleOrDefault(n => n.Q1 == q1 && n.Q2 == q2 && (Session.Character.UseSp ? n.Morph == Session.Character.Morph : n.Morph == 0));

                //        qlFrom.Q1 = q1;
                //        qlFrom.Q2 = q2;

                //        if (qlTo == null)
                //        {
                //            // Put 'from' to new position (datax)
                //            session.SendPacket($"qset {qlFrom.Q1} {qlFrom.Q2} {qlFrom.Type}.{qlFrom.Slot}.{qlFrom.Pos}.0");

                //            // old 'from' is now empty.
                //            session.SendPacket($"qset {data1} {data2} 7.7.-1.0");
                //        }
                //        else
                //        {
                //            // Put 'from' to new position (datax)
                //            session.SendPacket($"qset {qlFrom.Q1} {qlFrom.Q2} {qlFrom.Type}.{qlFrom.Slot}.{qlFrom.Pos}.0");

                //            // 'from' is now 'to' because they exchanged
                //            qlTo.Q1 = data1;
                //            qlTo.Q2 = data2;
                //            session.SendPacket($"qset {qlTo.Q1} {qlTo.Q2} {qlTo.Type}.{qlTo.Slot}.{qlTo.Pos}.0");
                //        }
                //    }

                //    break;

                //case 3:
                //    // Remove from Quicklist
                //    session.Character.QuicklistEntries.RemoveAll(n => n.Q1 == q1 && n.Q2 == q2 && (Session.Character.UseSp ? n.Morph == Session.Character.Morph : n.Morph == 0));
                //    session.SendPacket($"qset {q1} {q2} 7.7.-1.0");
                //    break;

                //default:
                //    return;
            }
        }
    }
}
