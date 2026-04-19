//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.GroupService;
using NosCore.Networking.SessionGroup;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class GroupTests
    {
        private Group Group = null!;
        private List<ClientSession> Sessions = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Group = new Group(GroupType.Group, TestHelpers.Instance.SessionGroupFactory)
            {
                GroupId = new IdService<Group>(1).GetNextId()
            };
            Sessions = new List<ClientSession>();
        }

        private async Task<ClientSession> CreateCharacterAsync()
        {
            var session = await TestHelpers.Instance.GenerateSessionAsync();
            Sessions.Add(session);
            return session;
        }

        [TestMethod]
        public async Task AddingSinglePlayerToGroupShouldNotFormFullGroup()
        {
            await new Spec("Adding single player to group should not form full group")
                .WhenAsync(ASinglePlayerJoins)
                .Then(GroupShouldNotHave_Members, 2)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RemovingOnlyPlayerShouldLeaveGroupEmpty()
        {
            await new Spec("Removing only player should leave group empty")
                .GivenAsync(AGroupWithOnePlayer)
                .When(ThePlayerLeaves)
                .Then(GroupShouldBeEmpty)
                .ExecuteAsync();
        }

        [TestMethod]
        public void PetsCannotJoinGroups()
        {
            new Spec("Pets cannot join groups")
                .When(APetAttemptsToJoin)
                .Then(GroupShouldRemainEmpty)
                .Execute();
        }

        [TestMethod]
        public async Task LeaderShouldTransferWhenOriginalLeaderLeaves()
        {
            await new Spec("Leader should transfer when original leader leaves")
                .GivenAsync(AFullGroupWithALeader)
                .Then(GroupShouldBeFullAndHaveALeader)
                .When(TheLeaderLeaves)
                .Then(LeadershipShouldTransferToNextMember)
                .ExecuteAsync();
        }

        private async Task ASinglePlayerJoins()
        {
            var session = await CreateCharacterAsync();
            Group.JoinGroup(session.Character);
        }

        private void GroupShouldNotHave_Members(int value)
        {
            Assert.IsFalse(Group.Count == 2);
        }

        private async Task AGroupWithOnePlayer()
        {
            var session = await CreateCharacterAsync();
            Group.JoinGroup(session.Character);
        }

        private void ThePlayerLeaves()
        {
            var player = Group.ElementAt(0).Value.Item2;
            Group.LeaveGroup(player);
        }

        private void GroupShouldBeEmpty()
        {
            Assert.AreEqual(0, Group.Count);
        }

        private void APetAttemptsToJoin()
        {
            var pet = new Mock<INamedEntity>();
            pet.SetupGet(s => s.VisualType).Returns(VisualType.Monster);
            pet.SetupGet(s => s.VisualId).Returns(1);
            Group.JoinGroup(pet.Object);
        }

        private void GroupShouldRemainEmpty()
        {
            Assert.IsTrue(Group.IsEmpty);
        }

        private async Task AFullGroupWithALeader()
        {
            for (var i = 0; i < (long)Group.Type; i++)
            {
                var session = await CreateCharacterAsync();
                Group.JoinGroup(session.Character);
            }
        }

        private void GroupShouldBeFullAndHaveALeader()
        {
            Assert.IsTrue(Group.IsGroupFull);
            Assert.IsTrue(Group.IsGroupLeader(Group.ElementAt(0).Value.Item2.VisualId));
        }

        private void TheLeaderLeaves()
        {
            Group.LeaveGroup(Group.ElementAt(0).Value.Item2);
        }

        private void LeadershipShouldTransferToNextMember()
        {
            Assert.IsFalse(Group.IsGroupFull);
            Assert.IsTrue(Group.IsGroupLeader(Group.ElementAt(1).Value.Item2.VisualId));
        }
    }
}
