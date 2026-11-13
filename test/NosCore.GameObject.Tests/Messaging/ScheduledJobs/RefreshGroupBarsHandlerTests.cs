//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Messaging.ScheduledJobs;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BroadcastService;
using NosCore.GameObject.Services.GroupService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Messaging.ScheduledJobs
{
    [TestClass]
    public class RefreshGroupBarsHandlerTests
    {
        private TestWorld World;
        private ClientSession First;
        private ClientSession Second;
        private ClientSession Loner;
        private readonly IdService<Group> Groups = new(1);

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            First = await World.CreateSessionAsync();
            Second = await World.CreateSessionAsync();
            Loner = await World.CreateSessionAsync();
            World.Inject<IIdService<Group>>(Groups);
            World.Inject<ISessionRegistry>(World.SessionRegistry);
        }

        [TestMethod]
        public async Task EachMemberIsSentTheBarsOfTheOthers()
        {
            await new Spec("Each member of a group is sent the HP and MP of the others, not its own")
                .Given(TheFirstTwoAreInAGroup)
                .And(TheSecondIsAt_HpOf_, 50, 200)
                .WhenAsync(TheBarsAreRefreshed)
                .Then(TheFirstShouldSeeTheSecondAt_Percent, 25)
                .And(NobodyShouldBeSentTheirOwnBar)
                .And(TheLonerShouldBeSentNothing)
                .ExecuteAsync();
        }

        private void TheFirstTwoAreInAGroup()
        {
            var sessionGroupFactory = new Mock<ISessionGroupFactory>();
            sessionGroupFactory.Setup(f => f.Create()).Returns(new Mock<ISessionGroup>().Object);
            var group = new Group(GroupType.Group, sessionGroupFactory.Object) { GroupId = Groups.GetNextId() };
            group.JoinGroup(First.Character);
            group.JoinGroup(Second.Character);
            First.Character.Group = group;
            Second.Character.Group = group;
            Groups.Items[group.GroupId] = group;
        }

        private void TheSecondIsAt_HpOf_(int hp, int maxHp)
        {
            Second.Character.MaxHp = maxHp;
            Second.Character.Hp = hp;
        }

        private Task TheBarsAreRefreshed()
        {
            First.LastPackets.Clear();
            Second.LastPackets.Clear();
            Loner.LastPackets.Clear();
            return World.Create<RefreshGroupBarsHandler>().Handle(new RefreshGroupBarsMessage());
        }

        private void TheFirstShouldSeeTheSecondAt_Percent(int percent) =>
            Assert.AreEqual(percent, First.LastPackets.OfType<PstPacket>().Single(p => p.VisualId == Second.Character.VisualId).HpLeft);

        private void NobodyShouldBeSentTheirOwnBar()
        {
            Assert.IsFalse(First.LastPackets.OfType<PstPacket>().Any(p => p.VisualId == First.Character.VisualId));
            Assert.IsFalse(Second.LastPackets.OfType<PstPacket>().Any(p => p.VisualId == Second.Character.VisualId));
        }

        private void TheLonerShouldBeSentNothing() =>
            Assert.IsFalse(Loner.LastPackets.OfType<PstPacket>().Any());
    }
}
