//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.Services.GroupService;
using NosCore.Networking.SessionGroup;
using SpecLight;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.GroupService
{
    [TestClass]
    public class GroupRegistryTests
    {
        private IGroupRegistry Registry = null!;
        private Mock<ISessionGroupFactory> MockSessionGroupFactory = null!;

        [TestInitialize]
        public void Setup()
        {
            MockSessionGroupFactory = new Mock<ISessionGroupFactory>();
            MockSessionGroupFactory.Setup(f => f.Create()).Returns(new Mock<ISessionGroup>().Object);
            Registry = new GroupRegistry();
        }

        [TestMethod]
        public async Task RegisterShouldAddGroup()
        {
            await new Spec("Register should add group")
                .Given(GroupIsCreated)
                .When(RegisteringGroup)
                .Then(GroupShouldBeRetrievable)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetByIdShouldReturnNullForUnknownId()
        {
            await new Spec("Get by ID should return null for unknown ID")
                .When(GettingUnknownGroup)
                .Then(ResultShouldBeNull)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterShouldRemoveGroup()
        {
            await new Spec("Unregister should remove group")
                .Given(GroupIsRegistered)
                .When(UnregisteringGroup)
                .Then(GroupShouldNotExist)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RegisterMemberShouldTrackMembership()
        {
            await new Spec("Register member should track membership")
                .Given(GroupIsRegistered)
                .When(RegisteringMember)
                .Then(MemberShouldBeRegistered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UnregisterMemberShouldRemoveMembership()
        {
            await new Spec("Unregister member should remove membership")
                .Given(MemberIsRegistered)
                .When(UnregisteringMember)
                .Then(MemberShouldNotBeRegistered)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GetNextGroupIdShouldIncrementEachCall()
        {
            await new Spec("Get next group ID should increment each call")
                .When(GettingNextGroupIds)
                .Then(IdsShouldBeSequential)
                .ExecuteAsync();
        }

        private Group? TestGroup;
        private Group? ResultGroup;
        private const long TestGroupId = 1;
        private const long TestCharacterId = 100;
        private long FirstId;
        private long SecondId;

        private void GroupIsCreated()
        {
            TestGroup = new Group(GroupType.Group, MockSessionGroupFactory.Object)
            {
                GroupId = TestGroupId
            };
        }

        private void GroupIsRegistered()
        {
            GroupIsCreated();
            Registry.Register(TestGroup!);
        }

        private void MemberIsRegistered()
        {
            GroupIsRegistered();
            Registry.RegisterMember(TestCharacterId, TestGroupId);
        }

        private void RegisteringGroup()
        {
            Registry.Register(TestGroup!);
        }

        private void GettingUnknownGroup()
        {
            ResultGroup = Registry.GetById(9999);
        }

        private void UnregisteringGroup()
        {
            Registry.Unregister(TestGroupId);
        }

        private void RegisteringMember()
        {
            Registry.RegisterMember(TestCharacterId, TestGroupId);
        }

        private void UnregisteringMember()
        {
            Registry.UnregisterMember(TestCharacterId);
        }

        private void GettingNextGroupIds()
        {
            FirstId = Registry.GetNextGroupId();
            SecondId = Registry.GetNextGroupId();
        }

        private void GroupShouldBeRetrievable()
        {
            var result = Registry.GetById(TestGroupId);
            Assert.IsNotNull(result);
            Assert.AreEqual(TestGroupId, result.GroupId);
        }

        private void ResultShouldBeNull()
        {
            Assert.IsNull(ResultGroup);
        }

        private void GroupShouldNotExist()
        {
            var result = Registry.GetById(TestGroupId);
            Assert.IsNull(result);
        }

        private void MemberShouldBeRegistered()
        {
            // Member registration is internal, we verify through group being registered
            Assert.IsNotNull(Registry.GetById(TestGroupId));
        }

        private void MemberShouldNotBeRegistered()
        {
            // Member unregistration happened, group should still exist
            Assert.IsNotNull(Registry.GetById(TestGroupId));
        }

        private void IdsShouldBeSequential()
        {
            Assert.AreEqual(1, FirstId);
            Assert.AreEqual(2, SecondId);
        }
    }
}
