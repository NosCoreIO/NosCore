//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.DignityService;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.HpService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.MpService;
using NosCore.Algorithm.ReputationService;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.Group;
using NosCore.GameObject.ComponentEntities.Entities;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.GroupService;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService;
using NosCore.GameObject.Services.SpeedCalculationService;
using NosCore.Networking.SessionGroup;
using NosCore.Tests.Shared;
using NosCore.Tests.Shared.AutoFixture;
using SpecLight;
using System.Linq;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class GroupTests
    {
        private Group Group = null!;
        private NosCoreFixture Fixture = null!;

        [TestInitialize]
        public void Setup()
        {
            Fixture = new NosCoreFixture();
            Group = new Group(GroupType.Group, new Mock<ISessionGroupFactory>().Object)
            {
                GroupId = new IdService<Group>(1).GetNextId()
            };
        }

        private Character CreateCharacter(long id = 1, string name = "TestCharacter")
        {
            return new Character(
                new Mock<IInventoryService>().Object,
                new Mock<IExchangeService>().Object,
                new Mock<IItemGenerationService>().Object,
                new HpService(),
                new MpService(),
                new ReputationService(),
                new DignityService(),
                new Mock<ISpeedCalculationService>().Object,
                TestHelpers.Instance.SessionRegistry,
                TestHelpers.Instance.GameLanguageLocalizer)
            {
                CharacterId = id,
                Name = name,
                Slot = 1,
                AccountId = id,
                MapId = 1,
                State = CharacterState.Active
            };
        }

        [TestMethod]
        public void AddingSinglePlayerToGroupShouldNotFormFullGroup()
        {
            new Spec("Adding single player to group should not form full group")
                .When(ASinglePlayerJoins)
                .Then(GroupShouldNotHave_Members, 2)
                .Execute();
        }

        [TestMethod]
        public void RemovingOnlyPlayerShouldLeaveGroupEmpty()
        {
            new Spec("Removing only player should leave group empty")
                .Given(AGroupWithOnePlayer)
                .When(ThePlayerLeaves)
                .Then(GroupShouldBeEmpty)
                .Execute();
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
        public void LeaderShouldTransferWhenOriginalLeaderLeaves()
        {
            new Spec("Leader should transfer when original leader leaves")
                .Given(AFullGroupWithALeader)
                .Then(GroupShouldBeFullAndHaveALeader)
                .When(TheLeaderLeaves)
                .Then(LeadershipShouldTransferToNextMember)
                .Execute();
        }

        private void ASinglePlayerJoins()
        {
            Group.JoinGroup(CreateCharacter());
        }

        private void GroupShouldNotHave_Members(int value)
        {
            Assert.IsFalse(Group.Count == 2);
        }

        private void AGroupWithOnePlayer()
        {
            var entity = CreateCharacter();
            Group.JoinGroup(entity);
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
            Group.JoinGroup(new Pet());
        }

        private void GroupShouldRemainEmpty()
        {
            Assert.IsTrue(Group.IsEmpty);
        }

        private void AFullGroupWithALeader()
        {
            for (var i = 0; i < (long)Group.Type; i++)
            {
                Group.JoinGroup(CreateCharacter(i + 1, $"TestCharacter{i}"));
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
