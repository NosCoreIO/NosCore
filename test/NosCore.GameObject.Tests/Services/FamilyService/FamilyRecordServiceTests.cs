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
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using System.Linq;
using System.Threading.Tasks;
using FamilyAuthority = NosCore.Data.Enumerations.Family.FamilyAuthority;
using FamilyEntity = NosCore.Database.Entities.Family;
using FamilyLogType = NosCore.Data.Enumerations.Family.FamilyLogType;
using FamilyMemberRank = NosCore.Data.Enumerations.Family.FamilyMemberRank;
using GenderType = NosCore.Packets.Enumerations.GenderType;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyRecordServiceTests
    {
        private const long HeadId = 1;
        private const long DeputyId = 2;
        private const long OutsiderId = 3;

        private TestWorld World;

        private IDao<FamilyDto, long> _familyDao;
        private IDao<FamilyCharacterDto, long> _familyCharacterDao;
        private IDao<FamilyLogDto, long> _familyLogDao;
        private FamilyRecordService _service;
        private long _familyId;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            var context = World.DbContextBuilder;
            _familyDao = new Dao<FamilyEntity, FamilyDto, long>(NullLogger<Dao<FamilyEntity, FamilyDto, long>>.Instance, context);
            _familyCharacterDao = new Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>(
                NullLogger<Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>>.Instance, context);
            _familyLogDao = new Dao<Database.Entities.FamilyLog, FamilyLogDto, long>(
                NullLogger<Dao<Database.Entities.FamilyLog, FamilyLogDto, long>>.Instance, context);
            _familyId = (await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Testers", FamilyLevel = 1, MaxSize = 3, FamilyMessage = string.Empty
            })).NotNull().FamilyId;
            await _familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = _familyId, CharacterId = HeadId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty
            });
            await _familyCharacterDao.TryInsertOrUpdateAsync(new FamilyCharacterDto
            {
                FamilyId = _familyId, CharacterId = DeputyId, Authority = FamilyAuthority.Assistant, DailyMessage = string.Empty
            });
            World.Inject(_familyDao);
            World.Inject(_familyCharacterDao);
            World.Inject(_familyLogDao);
            _service = World.Create<FamilyRecordService>();
        }

        private async Task<FamilyDto?> FamilyAsync() => await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId);

        private async Task<FamilyCharacterDto?> MemberAsync(long characterId) =>
            await _familyCharacterDao.FirstOrDefaultAsync(s => s.CharacterId == characterId);

        [TestMethod]
        public async Task ANoticeTouchesNothingElseOnTheFamily()
        {
            var family = (await FamilyAsync()).NotNull();
            family.FamilyLevel = 4;
            family.FamilyExperience = 1200;
            await _familyDao.TryInsertOrUpdateAsync(family);

            Assert.IsTrue(await _service.ChangeAsync(_familyId, new FamilyChange { FamilyMessage = "meet at the bank" }, []));

            var stored = (await FamilyAsync()).NotNull();
            Assert.AreEqual("meet at the bank", stored.FamilyMessage);
            Assert.AreEqual(4, stored.FamilyLevel);
            Assert.AreEqual(1200, stored.FamilyExperience);
        }

        [TestMethod]
        public async Task ATitleKeepsTheMembersContribution()
        {
            var membership = (await MemberAsync(DeputyId)).NotNull();
            membership.Experience = 500;
            await _familyCharacterDao.TryInsertOrUpdateAsync(membership);

            Assert.IsTrue(await _service.ChangeAsync(_familyId, null,
                [new FamilyMemberChange(DeputyId) { Rank = FamilyMemberRank.Uncle }]));

            var stored = (await MemberAsync(DeputyId)).NotNull();
            Assert.AreEqual(FamilyMemberRank.Uncle, stored.Rank);
            Assert.AreEqual(500, stored.Experience);
            Assert.AreEqual(FamilyAuthority.Assistant, stored.Authority);
        }

        [TestMethod]
        public async Task TheHeadsGenderAndEveryTitleChangeTogether()
        {
            Assert.IsTrue(await _service.ChangeAsync(_familyId, null,
                [new FamilyMemberChange(DeputyId) { Rank = FamilyMemberRank.Uncle }]));

            Assert.IsTrue(await _service.ChangeAsync(_familyId, new FamilyChange { FamilyHeadGender = GenderType.Female },
            [
                new FamilyMemberChange(HeadId) { Rank = FamilyMemberRank.Nothing },
                new FamilyMemberChange(DeputyId) { Rank = FamilyMemberRank.Nothing }
            ]));

            Assert.AreEqual(GenderType.Female, (await FamilyAsync()).NotNull().FamilyHeadGender);
            Assert.AreEqual(FamilyMemberRank.Nothing, (await MemberAsync(DeputyId)).NotNull().Rank);
        }

        [TestMethod]
        public async Task AFamilyThatIsGoneIsNotRecreated()
        {
            Assert.IsFalse(await _service.ChangeAsync(_familyId + 1, new FamilyChange { FamilyMessage = "hello" }, []));
            Assert.IsNull(await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId + 1));
        }

        [TestMethod]
        public async Task AMemberWhoMovedToAnotherFamilyIsNotChanged()
        {
            Assert.IsFalse(await _service.ChangeAsync(_familyId + 1, null,
                [new FamilyMemberChange(DeputyId) { Rank = FamilyMemberRank.Uncle }]));

            Assert.AreNotEqual(FamilyMemberRank.Uncle, (await MemberAsync(DeputyId)).NotNull().Rank);
        }

        [TestMethod]
        public async Task NothingIsWrittenWhenOneOfTheMembersHasLeft()
        {
            Assert.IsFalse(await _service.ChangeAsync(_familyId, new FamilyChange { FamilyMessage = "hello" },
            [
                new FamilyMemberChange(DeputyId) { Authority = FamilyAuthority.Head },
                new FamilyMemberChange(OutsiderId) { Authority = FamilyAuthority.Assistant }
            ]));

            Assert.AreEqual(string.Empty, (await FamilyAsync()).NotNull().FamilyMessage);
            Assert.AreEqual(FamilyAuthority.Assistant, (await MemberAsync(DeputyId)).NotNull().Authority);
        }

        [TestMethod]
        public async Task AFailedSaveIsReported()
        {
            var familyCharacterDao = new Mock<IDao<FamilyCharacterDto, long>>();
            familyCharacterDao.Setup(s => s.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<FamilyCharacterDto, bool>>>()))
                .ReturnsAsync(new FamilyCharacterDto { CharacterId = DeputyId, FamilyId = _familyId });
#nullable disable
            familyCharacterDao.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<FamilyCharacterDto>()))
                .Returns(Task.FromResult<FamilyCharacterDto>(null));
#nullable restore
            var service = new FamilyRecordService(_familyDao, familyCharacterDao.Object, _familyLogDao,
                new Mock<IDaoTransactionScope> { DefaultValue = DefaultValue.Mock }.Object);

            Assert.IsFalse(await service.ChangeAsync(_familyId, null,
                [new FamilyMemberChange(DeputyId) { Authority = FamilyAuthority.Head }]));
        }

        [TestMethod]
        public async Task ANewMemberJoinsAsAMember()
        {
            Assert.IsTrue(await _service.AddMemberAsync(_familyId, OutsiderId));

            var stored = (await MemberAsync(OutsiderId)).NotNull();
            Assert.AreEqual(_familyId, stored.FamilyId);
            Assert.AreEqual(FamilyAuthority.Member, stored.Authority);
        }

        [TestMethod]
        public async Task SomeoneAlreadyInAFamilyIsNotAddedToAnother()
        {
            var other = (await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Others", FamilyLevel = 1, MaxSize = 50, FamilyMessage = string.Empty
            })).NotNull();

            Assert.IsFalse(await _service.AddMemberAsync(other.FamilyId, DeputyId));

            Assert.AreEqual(_familyId, (await MemberAsync(DeputyId)).NotNull().FamilyId);
        }

        [TestMethod]
        public async Task AFullFamilyTakesNobody()
        {
            Assert.IsTrue(await _service.AddMemberAsync(_familyId, OutsiderId));

            Assert.IsFalse(await _service.AddMemberAsync(_familyId, OutsiderId + 1));

            Assert.IsNull(await MemberAsync(OutsiderId + 1));
        }

        [TestMethod]
        public async Task NobodyJoinsAFamilyThatIsGone()
        {
            Assert.IsFalse(await _service.AddMemberAsync(_familyId + 1, OutsiderId));

            Assert.IsNull(await MemberAsync(OutsiderId));
        }

        [TestMethod]
        public async Task ALeaverIsOnlyRemovedFromTheFamilyTheyWereIn()
        {
            Assert.IsFalse(await _service.RemoveMemberAsync(_familyId + 1, DeputyId));
            Assert.IsNotNull(await MemberAsync(DeputyId));

            Assert.IsTrue(await _service.RemoveMemberAsync(_familyId, DeputyId));
            Assert.IsNull(await MemberAsync(DeputyId));
        }

        [TestMethod]
        public async Task AFamilyIsFoundedWithItsHeadAndDeputies()
        {
            var familyId = await _service.CreateAsync("Founders", 50, 10, [11, 12]);

            Assert.IsNotNull(familyId);
            var members = _familyCharacterDao.Where(s => s.FamilyId == familyId).NotNull().ToList();
            Assert.AreEqual(3, members.Count);
            Assert.AreEqual(FamilyAuthority.Head, members.Single(s => s.CharacterId == 10).Authority);
            Assert.IsTrue(members.Where(s => s.CharacterId != 10).All(s => s.Authority == FamilyAuthority.Assistant));
        }

        [TestMethod]
        public async Task AFamilyNameIsOnlyTakenOnce()
        {
            Assert.IsNull(await _service.CreateAsync("Testers", 50, 10, [11, 12]));

            Assert.IsNull(await MemberAsync(10));
        }

        [TestMethod]
        public async Task AFounderAlreadyInAFamilyStopsTheFounding()
        {
            Assert.IsNull(await _service.CreateAsync("Founders", 50, 10, [DeputyId, 12]));

            Assert.IsNull(await _familyDao.FirstOrDefaultAsync(s => s.Name == "Founders"));
            Assert.AreEqual(_familyId, (await MemberAsync(DeputyId)).NotNull().FamilyId);
        }

        [TestMethod]
        public async Task DisbandingDeletesTheHistoryTheMembersAndTheFamily()
        {
            await _familyLogDao.TryInsertOrUpdateAsync(new FamilyLogDto { FamilyId = _familyId, FamilyLogType = FamilyLogType.DailyMessage });

            Assert.IsTrue(await _service.DisbandAsync(_familyId));

            Assert.IsNull(await FamilyAsync());
            Assert.IsNull(await MemberAsync(HeadId));
            Assert.IsNull(await MemberAsync(DeputyId));
            Assert.IsNull(await _familyLogDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId));
        }
    }
}
