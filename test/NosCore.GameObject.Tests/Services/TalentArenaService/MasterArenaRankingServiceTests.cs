//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Data.Dto;
using NosCore.GameObject.Services.TalentArenaService;
using NosCore.Shared.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.TalentArenaService
{
    [TestClass]
    public class MasterArenaRankingServiceTests
    {
        private readonly List<CharacterDto> _characters = new();
        private TARankPacket? _ranking;

        [TestMethod]
        public void EachClassListsItsBestThree()
        {
            new Spec("Each class shows its three characters with the most master points, best first")
                .Given(A_With_MasterPoints, "Ann", CharacterClassType.Swordsman, 2)
                .And(A_With_MasterPoints, "Bob", CharacterClassType.Swordsman, 7)
                .And(A_With_MasterPoints, "Cid", CharacterClassType.Swordsman, 4)
                .And(A_With_MasterPoints, "Dan", CharacterClassType.Swordsman, 1)
                .And(A_With_MasterPoints, "Eve", CharacterClassType.Mage, 3)
                .When(TheRankingIsBuilt)
                .Then(Place_ShouldBe_, 0, "Bob")
                .And(Place_ShouldBe_, 1, "Cid")
                .And(Place_ShouldBe_, 2, "Ann")
                .And(Place_ShouldBe_, 6, "Eve")
                .Execute();
        }

        [TestMethod]
        public void PlacesWithoutPointsStayEmpty()
        {
            new Spec("A class with fewer than three scorers leaves its other places empty, and nobody without points is listed")
                .Given(A_With_MasterPoints, "Ann", CharacterClassType.Archer, 0)
                .And(A_With_MasterPoints, "Bob", CharacterClassType.Archer, 5)
                .When(TheRankingIsBuilt)
                .Then(Place_ShouldBe_, 3, "Bob")
                .And(Place_ShouldBe_, 4, (string?)null)
                .And(TheRankingShouldHave_Places, 16)
                .Execute();
        }

        private void A_With_MasterPoints(string name, CharacterClassType type, int points) =>
            _characters.Add(new CharacterDto { CharacterId = _characters.Count + 1, Name = name, Class = type, MasterPoints = points, Level = 80 });

        private void TheRankingIsBuilt() =>
            _ranking = MasterArenaRankingService.Build(_characters, new LocalDateTime(2026, 11, 15, 11, 31));

        private void Place_ShouldBe_(int place, string? name) =>
            Assert.AreEqual(name, _ranking?.CharacterList?[place]?.Name);

        private void TheRankingShouldHave_Places(int count) => Assert.AreEqual(count, _ranking?.CharacterList?.Count);
    }
}
