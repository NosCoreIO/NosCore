//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ItemGeneration
{
    [TestClass]
    public class AmuletUsesTests
    {
        private const short AmuletVNum = 4262;

        private TestWorld World;
        private IItemInstance _amulet;

        [TestInitialize]
        public async Task SetupAsync() => World = await TestWorld.CreateAsync();

        [TestMethod]
        public async Task ARandomChampionBlessingAmuletIsCreatedWithItsUses()
        {
            await new Spec("A new random Champion Blessing Amulet can be used as many times as its data says")
                .Given(TheAmuletIsAnItemWith_Uses, 3)
                .When(TheAmuletIsCreated)
                .Then(ItShouldHave_UsesLeft, 3)
                .ExecuteAsync();
        }

        private void TheAmuletIsAnItemWith_Uses(int uses) => World.Inject(new List<ItemDto>
        {
            new Item
            {
                VNum = AmuletVNum, Type = NoscorePocketType.Equipment, ItemType = ItemType.Fashion,
                EquipmentSlot = EquipmentType.Amulet, Effect = ItemEffectType.RandomHeroic, EffectValue = uses,
            },
        });

        private void TheAmuletIsCreated() =>
            _amulet = World.Create<GameObject.Services.ItemGenerationService.ItemGenerationService>().Create(AmuletVNum, 1);

        private void ItShouldHave_UsesLeft(int uses) => Assert.AreEqual(uses, _amulet.DurabilityPoint);
    }
}
