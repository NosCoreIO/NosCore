//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.FamilyUpgradeService;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.FamilyService;
using NosCore.Tests.Shared;
using SpecLight;
using System.Linq;
using System.Threading.Tasks;
using FamilyEntity = NosCore.Database.Entities.Family;

namespace NosCore.GameObject.Tests.Services.FamilyService
{
    [TestClass]
    public class FamilyExtensionServiceTests
    {
        private const long HeadId = 1;
        private const long DeputyId = 2;
        private const short Warehouse = 9694;

        private IDao<FamilyDto, long> _familyDao;
        private FamilyExtensionService _service;
        private long _familyId;
        private FamilyExtensionPurchase[] _results;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            var context = TestHelpers.Instance.DbContextBuilder;
            _familyDao = new Dao<FamilyEntity, FamilyDto, long>(NullLogger<Dao<FamilyEntity, FamilyDto, long>>.Instance, context);
            var familyCharacterDao = new Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>(
                NullLogger<Dao<Database.Entities.FamilyCharacter, FamilyCharacterDto, long>>.Instance, context);
            var familyUpgradeDao = new Dao<Database.Entities.FamilyUpgrade, FamilyUpgradeDto, short>(
                NullLogger<Dao<Database.Entities.FamilyUpgrade, FamilyUpgradeDto, short>>.Instance, context);
            await familyUpgradeDao.TryInsertOrUpdateAsync(new FamilyUpgradeDto
            {
                ItemVNum = Warehouse, FamilyLevelMinimum = 2, UpgradeType = FamilyUpgradeType.Warehouse, UpgradeLevel = 0
            });
            _familyId = (await _familyDao.TryInsertOrUpdateAsync(new FamilyDto
            {
                Name = "Testers", FamilyLevel = 2, MaxSize = 50, FamilyMessage = string.Empty
            })).FamilyId;
            await familyCharacterDao.TryInsertOrUpdateAsync(new[]
            {
                new FamilyCharacterDto { FamilyId = _familyId, CharacterId = HeadId, Authority = FamilyAuthority.Head, DailyMessage = string.Empty },
                new FamilyCharacterDto { FamilyId = _familyId, CharacterId = DeputyId, Authority = FamilyAuthority.Assistant, DailyMessage = string.Empty }
            });
            _service = new FamilyExtensionService(_familyDao, familyCharacterDao, familyUpgradeDao, new FamilyUpgradeService());
        }

        [TestMethod]
        public async Task TheMasterChecksTheRankItHoldsNotTheOneTheChannelSent()
        {
            await new Spec("A deputy whose channel still thinks they lead is refused by the master")
                .WhenAsync(Purchasing_By_, Warehouse, DeputyId)
                .Then(TheResultsShouldBe_, "NotHead")
                .AndAsync(TheWarehouseShouldHave_, (byte)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherFamilysHeadCannotBuyForThisOne()
        {
            await new Spec("A purchase naming a family the buyer is not in is refused")
                .WhenAsync(PurchasingForAnotherFamily)
                .Then(TheResultsShouldBe_, "NotInFamily")
                .AndAsync(TheWarehouseShouldHave_, (byte)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnItemTheManagerDoesNotSellIsRefused()
        {
            await new Spec("A vnum with no family upgrade row is not sold")
                .WhenAsync(Purchasing_By_, (short)1012, HeadId)
                .Then(TheResultsShouldBe_, "NotSold")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task PurchasesFromSeveralChannelsAreAppliedOnce()
        {
            await new Spec("Ten channels buying the warehouse at the same moment get one success")
                .WhenAsync(TenChannelsPurchaseAtOnce)
                .Then(ExactlyOnePurchaseShouldSucceed)
                .AndAsync(TheWarehouseShouldHave_, (byte)21)
                .ExecuteAsync();
        }

        private async Task Purchasing_By_(short vnum, long characterId) =>
            _results = new[] { await _service.PurchaseAsync(_familyId, characterId, vnum) };

        private async Task PurchasingForAnotherFamily() =>
            _results = new[] { await _service.PurchaseAsync(_familyId + 1, HeadId, Warehouse) };

        private async Task TenChannelsPurchaseAtOnce() =>
            _results = await Task.WhenAll(Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => _service.PurchaseAsync(_familyId, HeadId, Warehouse))));

        private void TheResultsShouldBe_(string expected) => Assert.AreEqual(expected, string.Join(" ", _results));

        private void ExactlyOnePurchaseShouldSucceed()
        {
            Assert.AreEqual(1, _results.Count(s => s == FamilyExtensionPurchase.Purchased));
            Assert.IsTrue(_results.Where(s => s != FamilyExtensionPurchase.Purchased).All(s => s == FamilyExtensionPurchase.AlreadyPurchased));
        }

        private async Task TheWarehouseShouldHave_(byte size) =>
            Assert.AreEqual(size, (await _familyDao.FirstOrDefaultAsync(s => s.FamilyId == _familyId)).NotNull().WarehouseSize);
    }
}
