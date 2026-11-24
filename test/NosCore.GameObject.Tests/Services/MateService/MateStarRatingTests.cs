//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MateService;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using MateServiceImpl = NosCore.GameObject.Services.MateService.MateService;

namespace NosCore.GameObject.Tests.Services.MateService
{
    [TestClass]
    public class MateStarRatingTests
    {
        private const long CharacterId = 42;
        private const short GoldenGloopyCakeVNum = 709;
        private const short PartnerVNum = 317;
        private const byte KindRating = 4;

        private TestWorld World;
        private MateServiceImpl Service;
        private List<Mate> _loaded = [];

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject<INpcMonsterCatalog>(new NpcMonsterCatalog(new List<NpcMonsterDto>
            {
                new() { NpcMonsterVNum = GoldenGloopyCakeVNum, Level = 1, MaxHp = 100, BaseStarRating = KindRating },
                new() { NpcMonsterVNum = PartnerVNum, Level = 1, MaxHp = 100 }
            }));
            World.UseReal<IMateStatsService, MateStatsService>();
            Service = World.Create<MateServiceImpl>();
        }

        [TestMethod]
        public async Task ANewPetStartsAtItsKindsStarRatingAsync()
        {
            await new Spec("A pet without a stored rating starts at its kind's rating")
                .GivenAsync(APetStoredWithRating_, (byte)0)
                .WhenAsync(LoadingAsync)
                .Then(ItsRatingIs_, KindRating)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AStoredRatingAboveTheKindsIsKeptAsync()
        {
            await new Spec("A stored rating above the kind's is kept")
                .GivenAsync(APetStoredWithRating_, (byte)6)
                .WhenAsync(LoadingAsync)
                .Then(ItsRatingIs_, (byte)6)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task APartnerHasNoStarRatingAsync()
        {
            await new Spec("A partner has no star rating")
                .GivenAsync(APartner)
                .WhenAsync(LoadingAsync)
                .Then(ItsRatingIs_, (byte)0)
                .ExecuteAsync();
        }

        [TestMethod]
        public void AnAdoptedPetStartsAtItsKindsStarRating()
        {
            var monster = new NpcMonsterDto { NpcMonsterVNum = GoldenGloopyCakeVNum, Level = 1, MaxHp = 100, BaseStarRating = KindRating };

            var mate = Service.Adopt(new MateDto { CharacterId = CharacterId, VNum = GoldenGloopyCakeVNum, MateType = MateType.Pet, Level = 1 },
                monster, []);

            Assert.AreEqual(KindRating, mate.StarRating);
        }

        private Task APetStoredWithRating_(byte storedRating) =>
            OwningAsync(GoldenGloopyCakeVNum, MateType.Pet, storedRating);

        private Task APartner() => OwningAsync(PartnerVNum, MateType.Partner, 0);

        private Task OwningAsync(short vnum, MateType type, byte starRating) =>
            World.MateDao.TryInsertOrUpdateAsync(new MateDto
            {
                CharacterId = CharacterId, VNum = vnum, MateType = type, Level = 1, StarRating = starRating
            });

        private async Task LoadingAsync() => _loaded = await Service.LoadAsync(CharacterId);

        private void ItsRatingIs_(byte rating)
        {
            var mate = _loaded.Single();
            Assert.AreEqual(rating, mate.StarRating);
            Assert.AreEqual(rating, (byte)mate.GenerateScp(RegionType.EN).Unknow3);
            Assert.AreEqual(rating, mate.GenerateIn(RegionType.EN, 0, 0).InNonPlayerSubPacket.NotNull().Unknow7);
        }
    }
}
