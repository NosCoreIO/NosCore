//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.FamilyExperienceService;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using System.Threading.Tasks;
using FamilyEntity = NosCore.Database.Entities.Family;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyProgressionServiceTests
    {
        private TestWorld World;
        private const long MemberId = 1;
        private const long OutsiderId = 2;

        private readonly FamilyExperienceService _curve = new();
        private IDao<FamilyDto, long> _familyDao;
        private IDao<FamilyCharacterDto, long> _familyCharacterDao;
        private FamilyProgressionService _service;
        private long _familyId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var context = World.DbContextBuilder;
            _familyDao = new Dao<FamilyEntity, FamilyDto, long>(NullLogger<Dao<FamilyEntity, FamilyDto, long>>.Instance, context);
            _familyCharacterDao = new Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>(
                NullLogger<Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>>.Instance, context);
            _familyId = (await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Testers", FamilyLevel = 1, MaxSize = 50, FamilyMessage = string.Empty
            })).NotNull().FamilyId;
            await _familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = _familyId, CharacterId = MemberId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty
            });
            _service = new FamilyProgressionService(_familyDao, _familyCharacterDao, _curve, new DaoTransactionScope(context));
        }

        private async Task<FamilyDto?> FamilyAsync() => await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId);

        [TestMethod]
        public async Task TheFamilyAndTheMemberBothGain()
        {
            var gain = await _service.AddExperienceAsync(_familyId, MemberId, 600);

            Assert.IsNotNull(gain);
            Assert.IsFalse(gain.LeveledUp);
            Assert.AreEqual(600, (await FamilyAsync()).NotNull().FamilyExperience);
            Assert.AreEqual(600, (await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == MemberId)).NotNull().Experience);
        }

        [TestMethod]
        public async Task FillingTheBarLevelsTheFamilyUp()
        {
            var bar = (int)_curve.GetFamilyExperience(1);

            var gain = (await _service.AddExperienceAsync(_familyId, MemberId, bar + 400)).NotNull();

            Assert.IsTrue(gain.LeveledUp);
            Assert.AreEqual(2, gain.FamilyLevel);
            var family = (await FamilyAsync()).NotNull();
            Assert.AreEqual(2, family.FamilyLevel);
            Assert.AreEqual(400, family.FamilyExperience);
        }

        [TestMethod]
        public async Task TheLastLevelKeepsAFullBar()
        {
            var family = (await FamilyAsync()).NotNull();
            family.FamilyLevel = 30;
            await _familyDao.TryInsertOrUpdateAsync(family);

            var gain = (await _service.AddExperienceAsync(_familyId, MemberId, (int)_curve.GetFamilyExperience(30))).NotNull();

            Assert.IsFalse(gain.LeveledUp);
            Assert.AreEqual(30, gain.FamilyLevel);
        }

        [TestMethod]
        public async Task SomeoneOutsideTheFamilyAddsNothing()
        {
            var gain = await _service.AddExperienceAsync(_familyId, OutsiderId, 600);

            Assert.IsNull(gain);
            Assert.AreEqual(0, (await FamilyAsync()).NotNull().FamilyExperience);
        }

        [TestMethod]
        public async Task AFailedSaveReportsNoGain()
        {
            var familyCharacterDao = new Mock<IDao<FamilyCharacterDto, long>>();
            familyCharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<FamilyCharacterDto, bool>>>()))
                .ReturnsAsync(new FamilyCharacterDto { CharacterId = MemberId, FamilyId = _familyId });
#nullable disable
            familyCharacterDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyCharacterDto>()))
                .Returns(Task.FromResult<FamilyCharacterDto>(null));
#nullable restore
            var service = new FamilyProgressionService(_familyDao, familyCharacterDao.Object, _curve,
                new Mock<IDaoTransactionScope> { DefaultValue = DefaultValue.Mock }.Object);

            Assert.IsNull(await service.AddExperienceAsync(_familyId, MemberId, 600));
        }
    }
}
