//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.SkillService;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.SkillService
{
    [TestClass]
    public class SkillServiceTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private ISkillService Service = null!;
        private Mock<IDao<CharacterSkillDto, Guid>> CharacterSkillDao = null!;
        private List<SkillDto> Skills = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();

            CharacterSkillDao = new Mock<IDao<CharacterSkillDto, Guid>>();
            Skills = new List<SkillDto>
            {
                new SkillDto { SkillVNum = 1 },
                new SkillDto { SkillVNum = 2 },
                new SkillDto { SkillVNum = 3 }
            };

            Service = new GameObject.Services.SkillService.SkillService(
                CharacterSkillDao.Object,
                Skills);
        }

        [TestMethod]
        public async Task LoadSkillShouldLoadCharacterSkills()
        {
            await new Spec("Load skill should load character skills")
                .Given(CharacterHasSkillsInDatabase)
                .WhenAsync(LoadingSkills)
                .Then(CharacterShouldHaveSkills)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LoadSkillWithNoSkillsShouldClearSkills()
        {
            await new Spec("Load skill with no skills should clear skills")
                .Given(CharacterHasNoSkillsInDatabase)
                .WhenAsync(LoadingSkills)
                .Then(CharacterShouldHaveNoSkills)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ServiceCanBeConstructed()
        {
            await new Spec("Service can be constructed")
                .Then(ServiceShouldNotBeNull)
                .ExecuteAsync();
        }

        private void CharacterHasSkillsInDatabase()
        {
            var characterSkills = new List<CharacterSkillDto>
            {
                new CharacterSkillDto { Id = Guid.NewGuid(), CharacterId = Session.Character.VisualId, SkillVNum = 1 },
                new CharacterSkillDto { Id = Guid.NewGuid(), CharacterId = Session.Character.VisualId, SkillVNum = 2 }
            };

            CharacterSkillDao.Setup(s => s.Where(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterSkillDto, bool>>>()))
                .Returns(characterSkills);
        }

        private void CharacterHasNoSkillsInDatabase()
        {
            CharacterSkillDao.Setup(s => s.Where(It.IsAny<System.Linq.Expressions.Expression<Func<CharacterSkillDto, bool>>>()))
                .Returns(new List<CharacterSkillDto>());
        }

        private async Task LoadingSkills()
        {
            await Service.LoadSkill(Session.Character);
        }

        private void CharacterShouldHaveSkills()
        {
            Assert.IsTrue(Session.Character.Skills.Count > 0);
        }

        private void CharacterShouldHaveNoSkills()
        {
            Assert.AreEqual(0, Session.Character.Skills.Count);
        }

        private void ServiceShouldNotBeNull()
        {
            Assert.IsNotNull(Service);
        }
    }
}
