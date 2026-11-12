//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Tests.Shared;
using System.Threading.Tasks;
using FamilyEntity = NosCore.Database.Entities.Family;
using FamilyServiceImpl = NosCore.GameObject.Services.FamilyService.FamilyService;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyServiceUpdateTests
    {
        private const long MemberId = 1;

        private TestWorld World;

        private IDao<FamilyDto, long> _familyDao;
        private IDao<FamilyCharacterDto, long> _familyCharacterDao;
        private FamilyServiceImpl _service;
        private long _familyId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var context = World.DbContextBuilder;
            _familyDao = new Dao<FamilyEntity, FamilyDto, long>(NullLogger<Dao<FamilyEntity, FamilyDto, long>>.Instance, context);
            _familyCharacterDao = new Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>(
                NullLogger<Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>>.Instance, context);
            var family = await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Testers", FamilyLevel = 1, MaxSize = 50, FamilyMessage = string.Empty
            });
            Assert.IsNotNull(family);
            _familyId = family.FamilyId;
            await _familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = _familyId, CharacterId = MemberId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty
            });
            _service = new FamilyServiceImpl(_familyDao, _familyCharacterDao, new Mock<IDao<FamilyLogDto, long>>().Object,
                World.CharacterDao);
        }

        [TestMethod]
        public async Task ANoticeKeepsALevelGainedSinceTheFamilyWasLoaded()
        {
            var loaded = await _service.GetFamilyAsync(MemberId);
            Assert.IsNotNull(loaded);
            await RaiseLevelAsync(4);

            Assert.IsTrue(await _service.UpdateAsync(_familyId, s => s.FamilyMessage = "meet at the bank"));

            var stored = await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId);
            Assert.IsNotNull(stored);
            Assert.AreEqual("meet at the bank", stored.FamilyMessage);
            Assert.AreEqual(4, stored.FamilyLevel);
        }

        [TestMethod]
        public async Task ATitleKeepsContributionGainedSinceTheFamilyWasLoaded()
        {
            var loaded = await _service.GetFamilyAsync(MemberId);
            Assert.IsNotNull(loaded);
            var membership = await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == MemberId);
            Assert.IsNotNull(membership);
            membership.Experience = 500;
            await _familyCharacterDao.TryInsertOrUpdateAsync(membership);

            Assert.IsTrue(await _service.UpdateMemberAsync(MemberId, s => s.Rank = FamilyMemberRank.Uncle));

            var stored = await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == MemberId);
            Assert.IsNotNull(stored);
            Assert.AreEqual(FamilyMemberRank.Uncle, stored.Rank);
            Assert.AreEqual(500, stored.Experience);
        }

        [TestMethod]
        public async Task AFamilyThatIsGoneIsNotRecreated()
        {
            Assert.IsFalse(await _service.UpdateAsync(_familyId + 1, s => s.FamilyMessage = "hello"));
            Assert.IsNull(await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId + 1));
        }

        private async Task RaiseLevelAsync(byte level)
        {
            var family = await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId);
            Assert.IsNotNull(family);
            family.FamilyLevel = level;
            await _familyDao.TryInsertOrUpdateAsync(family);
        }
    }
}
