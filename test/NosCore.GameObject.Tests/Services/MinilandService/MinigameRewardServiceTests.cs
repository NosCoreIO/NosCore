//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MinilandService;
using NosCore.GameObject.Services.UpgradeService;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Tests.Services.MinilandService
{
    [TestClass]
    public class MinigameRewardServiceTests
    {
        private readonly Mock<IRandomNumberSource> _random = new();

        private MinigameRewardService Service => new(
            new[] { 999, 4999, 7999, 11999, 15999, 1000000 }
                .Select((max, level) => new MinigameScoreDto
                    { MinigameScoreId = (short)level, MinigameType = MinigameType.Quarry, Level = (byte)level, MaximumScore = max })
                .ToList(),
            new List<MinigameRewardDto>
            {
                new() { MinigameRewardId = 0, MinigameVNum = 3117, Level = 1, ItemVNum = 1031, Amount = 2 },
                new() { MinigameRewardId = 1, MinigameVNum = 3117, Level = 1, ItemVNum = 2205, Amount = 1 },
                new() { MinigameRewardId = 2, MinigameVNum = 3121, Level = 1, ItemVNum = 2034, Amount = 3 },
            },
            _random.Object);

        [TestMethod]
        [DataRow(0, null)]
        [DataRow(999, null)]
        [DataRow(1000, 0)]
        [DataRow(8000, 2)]
        [DataRow(16000, 4)]
        public void AScoreEarnsTheBoxOfTheHighestBandItPasses(int score, int? box) =>
            Assert.AreEqual(box, (int?)Service.GetLevel(MinigameType.Quarry, score));

        [TestMethod]
        public void AMinigameWithoutBandsHasNone() =>
            Assert.AreEqual(0, Service.GetMaximumScores(MinigameType.Sawmill).Count);

        [TestMethod]
        [DataRow(0.0, (short)1031)]
        [DataRow(0.99, (short)2205)]
        public void ARewardIsDrawnFromItsMinigameAndBox(double roll, short vnum)
        {
            _random.Setup(r => r.NextDouble()).Returns(roll);

            Assert.AreEqual(vnum, Service.DrawReward(3117, 1)?.ItemVNum);
        }

        [TestMethod]
        public void ABoxWithoutRewardsGivesNothing() =>
            Assert.IsNull(Service.DrawReward(3117, 4));
    }
}
