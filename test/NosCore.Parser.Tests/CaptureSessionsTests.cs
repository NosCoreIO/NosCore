//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.MiniMap;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Portals;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Parser.Parsers;
using NosCore.Shared.Enumerations;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class CaptureSessionsTests
    {
        private static List<IPacket> Merge(params IPacket[] packets)
        {
            var list = packets.ToList();
            CaptureSessions.MergeRepeatedEntities(list);
            return list;
        }

        [TestMethod]
        public void AnNpcCapturedAgainUnderANewIdKeepsItsFirstId()
        {
            var copy = Captured.Npc(915, 1907, 90, 45);
            var effect = new EffectPacket { EffectType = VisualType.Npc, VisualEntityId = 1907, Id = 3010 };
            var shop = Captured.Shop(1907, 0, 0, "Shop");
            var stock = Captured.SkillShop(1907, 0, 220);
            var talk = Captured.Talk(NrunRunnerType.Teleport, 2, 1907);

            Merge(Captured.Arrival(1), Captured.Npc(915, 1903, 90, 45), Captured.Arrival(1), copy, effect, shop, stock, talk);

            Assert.AreEqual(1903, copy.VisualId);
            Assert.AreEqual(1903, effect.VisualEntityId);
            Assert.AreEqual(1903, shop.VisualId);
            Assert.AreEqual(1903, stock.VisualId);
            Assert.AreEqual(1903, talk.VisualId);
        }

        [TestMethod]
        public void AnIdAnotherNpcAlreadyHoldsIsNotReused()
        {
            var first = Captured.Npc(915, 1903, 90, 45);
            var other = Captured.Npc(1082, 1903, 97, 10);
            var otherAgain = Captured.Npc(1082, 2001, 97, 10);
            var shop = Captured.Shop(1903, 0, 0, "Shop");

            Merge(Captured.Arrival(1), first, Captured.Arrival(2), other, Captured.Arrival(2), otherAgain, shop);

            Assert.AreEqual(1903, first.VisualId);
            Assert.AreEqual(2001, other.VisualId);
            Assert.AreEqual(2001, otherAgain.VisualId);
            Assert.AreEqual(2001, shop.VisualId);
        }

        [TestMethod]
        public void AMonsterWalkingOntoAnotherOnesCellStaysItself()
        {
            var walked = Captured.Monster(24, 501, 10, 10);

            Merge(Captured.Arrival(1), Captured.Monster(24, 500, 10, 10), Captured.Monster(24, 501, 12, 12), walked);

            Assert.AreEqual(501, walked.VisualId);
        }

        [TestMethod]
        public void AShopCapturedBeforeItsNpcFollowsTheNpc()
        {
            var stock = Captured.SkillShop(1907, 1, 220, 221);

            Merge(Captured.Arrival(1), stock, Captured.Npc(915, 1903, 90, 45), Captured.Npc(915, 1907, 90, 45));

            Assert.AreEqual(1903, stock.VisualId);
        }

        [TestMethod]
        public void TheSameNpcOnAnotherCellKeepsItsId()
        {
            var next = Captured.Npc(915, 1904, 91, 45);

            Merge(Captured.Arrival(1), Captured.Npc(915, 1903, 90, 45), next);

            Assert.AreEqual(1904, next.VisualId);
        }

        [TestMethod]
        public void TheSameCellOnAnotherMapKeepsItsId()
        {
            var elsewhere = Captured.Npc(915, 1907, 90, 45);

            Merge(Captured.Arrival(1), Captured.Npc(915, 1903, 90, 45), Captured.Arrival(2), elsewhere);

            Assert.AreEqual(1907, elsewhere.VisualId);
        }

        [TestMethod]
        public void AMonsterIdIsNotTakenForAnNpcId()
        {
            var copy = Captured.Monster(24, 1950, 10, 10);
            var shop = Captured.Shop(1950, 0, 0, "Shop");

            Merge(Captured.Arrival(1), Captured.Monster(24, 1903, 10, 10), copy, shop);

            Assert.AreEqual(1903, copy.VisualId);
            Assert.AreNotEqual(1903, shop.VisualId);
        }

        [TestMethod]
        public void AnNpcWithAnotherIdLeavesTheContestedOneToTheNpcThatHasNoOther()
        {
            var first = Captured.Npc(915, 1903, 90, 45);
            var other = Captured.Npc(1082, 1903, 97, 10);

            Merge(Captured.Arrival(1), first, Captured.Npc(915, 1907, 90, 45), Captured.Arrival(2), other);

            Assert.AreEqual(1907, first.VisualId);
            Assert.AreEqual(1903, other.VisualId);
        }

        [TestMethod]
        public void AnNpcWhoseOnlyIdIsHeldGetsAFreeOne()
        {
            var first = Captured.Npc(915, 1, 90, 45);
            var other = Captured.Npc(1082, 1, 97, 10);
            var shop = Captured.Shop(1, 0, 0, "Shop");

            Merge(Captured.Arrival(1), first, Captured.Arrival(2), other, shop);

            Assert.AreEqual(1, first.VisualId);
            Assert.AreEqual(2, other.VisualId);
            Assert.AreEqual(2, shop.VisualId);
        }

        [TestMethod]
        public void AMonsterWhoseOnlyIdIsHeldNearAKeptOneOfItsKindIsNotImportedAgain()
        {
            var first = Captured.Monster(24, 500, 10, 10);
            var other = Captured.Monster(30, 501, 50, 50);

            var merged = Merge(Captured.Arrival(1), first, Captured.Arrival(2), other, Captured.Arrival(1), Captured.Monster(24, 501, 13, 12));

            CollectionAssert.AreEqual(new[] { first, other }, merged.OfType<InPacket>().ToArray());
        }

        [TestMethod]
        public void TwoMonstersOfAKindSeenTogetherAreBothKept()
        {
            var first = Captured.Monster(24, 500, 10, 10);
            var other = Captured.Monster(30, 501, 50, 50);
            var neighbour = Captured.Monster(24, 501, 13, 12);

            var merged = Merge(Captured.Arrival(1), first, Captured.Arrival(2), other, Captured.Arrival(1), Captured.Monster(24, 500, 10, 10), neighbour);

            CollectionAssert.Contains(merged, neighbour);
            Assert.AreEqual(1, neighbour.VisualId);
        }

        [TestMethod]
        public void AMonsterWhoseOnlyIdIsHeldFarFromItsKindGetsAFreeOne()
        {
            var other = Captured.Monster(25, 500, 30, 30);

            Merge(Captured.Arrival(1), Captured.Monster(24, 500, 10, 10), Captured.Arrival(2), other);

            Assert.AreEqual(1, other.VisualId);
        }

        [TestMethod]
        public void AShopBeforeItsNpcFollowsAnNpcOfItsOwnMap()
        {
            var shop = Captured.Shop(1903, 0, 0, "Shop");
            var npc = Captured.Npc(915, 1903, 90, 45);

            Merge(Captured.Arrival(2), Captured.Npc(1082, 1903, 97, 10), Captured.Npc(1082, 2001, 97, 10),
                Captured.Arrival(1), shop, npc);

            Assert.AreEqual(npc.VisualId, shop.VisualId);
        }

        [TestMethod]
        public void AShopOfAnNpcTheMapNeverShowsIsNotTiedToAnother()
        {
            var stock = Captured.SkillShop(1903, 0, 20, 24, 28);
            var talk = Captured.Talk(NrunRunnerType.OpenProduction, 17, 1903);

            Merge(Captured.Arrival(1), Captured.Npc(915, 1903, 90, 45), Captured.Arrival(2), stock, talk);

            Assert.AreEqual(-1, stock.VisualId);
            Assert.AreEqual(-1, talk.VisualId);
        }

        [TestMethod]
        public void ANewerCaptureReplacesWhatOlderOnesShowOnTheMapsItVisits()
        {
            var older = new List<IPacket>
            {
                Captured.Arrival(1), Captured.Npc(985, 1, 10, 10), Captured.Npc(985, 2, 10, 10), Captured.Monster(24, 3, 20, 20),
                Captured.Portal(5, 5, 2, PortalType.MapPortal),
                Captured.Arrival(3), Captured.Npc(320, 4, 30, 30),
            };
            var newer = new List<IPacket>
            {
                Captured.Arrival(1), Captured.Npc(985, 900, 10, 10), Captured.Npc(788, 901, 40, 40),
                Captured.Portal(6, 6, 2, PortalType.MapPortal),
            };

            var combined = CaptureSessions.Combine([older, newer]);

            CollectionAssert.AreEquivalent(new[] { (1, (short)985), (1, (short)788), (3, (short)320) }, Shown(combined));
            Assert.AreEqual((short)6, combined.OfType<GpPacket>().Single().SourceX);
            Assert.IsFalse(combined.OfType<InPacket>().Any(p => p.VisualType == VisualType.Monster));
        }

        [TestMethod]
        public void TheSameNpcInANewerCaptureKeepsItsId()
        {
            var shop = Captured.Shop(1, 0, 3, "Shop");
            var again = Captured.Npc(985, 900, 10, 10);

            CaptureSessions.Combine([[Captured.Arrival(1), Captured.Npc(985, 1, 10, 10), shop], [Captured.Arrival(1), again]]);

            Assert.AreEqual(1, again.VisualId);
            Assert.AreEqual(1, shop.VisualId);
        }

        private static List<(int Map, short VNum)> Shown(List<IPacket> packets)
        {
            short map = 0;
            var shown = new List<(int, short)>();
            foreach (var packet in packets)
            {
                if (packet is AtPacket at)
                {
                    map = at.MapId;
                }
                else if (packet is InPacket { VisualType: VisualType.Npc, VNum: { } vnum })
                {
                    shown.Add((map, short.Parse(vnum, CultureInfo.InvariantCulture)));
                }
            }

            return shown;
        }
    }
}
