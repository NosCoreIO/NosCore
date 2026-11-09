//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Ecs.Extensions
{
    [TestClass]
    public class QuicklistExtensionsTests
    {
        private ClientSession Session = null!;
        private IReadOnlyList<QsetClientPacket> Packets = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
        }

        [TestMethod]
        public async Task SettingASlotReplacesWhatWasThere()
        {
            await new Spec("Setting an occupied slot replaces its entry")
                .Given(SlotOne_Two_Holds_, (short)3, (short)4)
                .When(SettingSlotOne_Two_To_, (short)5, (short)6)
                .Then(SlotOne_Two_ShouldHold_, (short)5, (short)6)
                .And(TheQuicklistShouldHave_Entries, 1)
                .And(Packet_ShouldBe_At_Showing_, 0, QSetType.Set, (short)2, (short)5)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingASlotEmptiesIt()
        {
            await new Spec("Removing a slot empties it and resets the client slot")
                .Given(SlotOne_Two_Holds_, (short)3, (short)4)
                .When(RemovingSlotOne_Two_)
                .Then(TheQuicklistShouldHave_Entries, 0)
                .And(Packet_ShouldBe_At_Showing_, 0, QSetType.Reset, (short)2, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingOntoAnOccupiedSlotSwapsTheEntries()
        {
            await new Spec("Moving an entry onto an occupied slot swaps the two")
                .Given(SlotOne_Two_Holds_, (short)3, (short)4)
                .And(SlotOne_Three_Holds_, (short)4, (short)5)
                .When(MovingSlotTwoToSlotThree)
                .Then(SlotOne_Three_ShouldHold_, (short)3, (short)4)
                .And(SlotOne_Two_ShouldHold_, (short)4, (short)5)
                .And(Packet_ShouldBe_At_Showing_, 0, QSetType.Set, (short)3, (short)3)
                .And(Packet_ShouldBe_At_Showing_, 1, QSetType.Set, (short)2, (short)4)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingOntoAnEmptySlotEmptiesTheOrigin()
        {
            await new Spec("Moving an entry onto an empty slot leaves the origin reset")
                .Given(SlotOne_Two_Holds_, (short)3, (short)4)
                .When(MovingSlotTwoToSlotThree)
                .Then(SlotOne_Three_ShouldHold_, (short)3, (short)4)
                .And(TheQuicklistShouldHave_Entries, 1)
                .And(Packet_ShouldBe_At_Showing_, 1, QSetType.Reset, (short)2, (short)7)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task MovingAnEmptySlotDoesNothing()
        {
            await new Spec("Moving from an empty slot changes nothing")
                .When(MovingSlotTwoToSlotThree)
                .Then(TheQuicklistShouldHave_Entries, 0)
                .And(NoPacketShouldBeGenerated)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EachSpecialistFormKeepsItsOwnQuicklist()
        {
            await new Spec("An entry set in one specialist form is not touched from another")
                .Given(SlotOne_Two_Holds_, (short)3, (short)4)
                .And(TheCharacterWearsSpecialistMorph_, (short)7)
                .When(RemovingSlotOne_Two_)
                .Then(TheQuicklistShouldHave_Entries, 1)
                .ExecuteAsync();
        }

        private void SlotOne_Two_Holds_(short iconType, short iconVNum) =>
            Session.Character.SetQuicklistEntry(QSetType.Set, 1, 2, iconType, iconVNum);

        private void SlotOne_Three_Holds_(short iconType, short iconVNum) =>
            Session.Character.SetQuicklistEntry(QSetType.Set, 1, 3, iconType, iconVNum);

        private void TheCharacterWearsSpecialistMorph_(short morph)
        {
            Session.Character.UseSp = true;
            Session.Character.Morph = morph;
        }

        private void SettingSlotOne_Two_To_(short iconType, short iconVNum) =>
            Packets = [Session.Character.SetQuicklistEntry(QSetType.Set, 1, 2, iconType, iconVNum)];

        private void RemovingSlotOne_Two_() => Packets = [Session.Character.RemoveQuicklistEntry(1, 2)];

        private void MovingSlotTwoToSlotThree() => Packets = Session.Character.MoveQuicklistEntry(1, 2, 1, 3);

        private void SlotOne_Two_ShouldHold_(short iconType, short iconVNum) => SlotShouldHold(2, iconType, iconVNum);

        private void SlotOne_Three_ShouldHold_(short iconType, short iconVNum) => SlotShouldHold(3, iconType, iconVNum);

        private void SlotShouldHold(short slot, short iconType, short iconVNum)
        {
            var entry = Session.Character.QuicklistEntries.Single(e => e.QuickListIndex == 1 && e.Slot == slot);
            Assert.AreEqual((iconType, iconVNum), (entry.IconType, entry.IconVNum));
        }

        private void TheQuicklistShouldHave_Entries(int count) =>
            Assert.AreEqual(count, Session.Character.QuicklistEntries.Count);

        private void Packet_ShouldBe_At_Showing_(int index, QSetType type, short slot, short iconType)
        {
            var packet = Packets[index];
            Assert.AreEqual(type, packet.Data?.Type);
            Assert.AreEqual((short)1, packet.OriginQuickList);
            Assert.AreEqual(slot, packet.OriginQuickListSlot);
            Assert.AreEqual(iconType, packet.Data?.OriginQuickList);
        }

        private void NoPacketShouldBeGenerated() => Assert.AreEqual(0, Packets.Count);
    }
}
