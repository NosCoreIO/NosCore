//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.ItemGeneration
{
    [TestClass]
    public class StoredSpecialistTests
    {
        private const short Card = 912;

        private TestWorld World;
        private SpecialistInstanceDto _stored;
        private SpecialistInstance _loaded;

        [TestInitialize]
        public async Task SetupAsync() => World = await TestWorld.CreateAsync();

        [TestMethod]
        public async Task ACardStoredAtLevelZeroLoadsAtLevelOne()
        {
            await new Spec("A specialist card stored without a level loads at its first level")
                .Given(ACardStoredAtLevel_, (byte)0)
                .When(LoadingTheCard)
                .Then(TheCardShouldBeAtLevel_, (byte)1)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACardKeepsTheLevelItWasStoredAt()
        {
            await new Spec("A specialist card loads at the level it was stored at")
                .Given(ACardStoredAtLevel_, (byte)45)
                .When(LoadingTheCard)
                .Then(TheCardShouldBeAtLevel_, (byte)45)
                .ExecuteAsync();
        }

        private void ACardStoredAtLevel_(byte level) =>
            _stored = new SpecialistInstanceDto { Id = Guid.NewGuid(), ItemVNum = Card, Amount = 1, SpLevel = level };

        private void LoadingTheCard() => _loaded = (SpecialistInstance)World.GenerateItemProvider().Convert(_stored);

        private void TheCardShouldBeAtLevel_(byte level) => Assert.AreEqual(level, _loaded.SpLevel);
    }
}
