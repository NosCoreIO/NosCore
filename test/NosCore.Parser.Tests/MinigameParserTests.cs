//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.Miniland;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class MinigameParserTests
    {
        private readonly List<MinigameRewardDto> _existingRewards = new();
        private readonly List<MinigameRewardDto> _savedRewards = new();
        private readonly List<MinigameScoreDto> _savedScores = new();
        private readonly List<short> _deletedRewards = new();
        private MinigameParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var rewards = new Mock<IDao<MinigameRewardDto, short>>();
            rewards.Setup(s => s.LoadAll()).Returns(_existingRewards);
            rewards.Setup(s => s.TryDeleteAsync(It.IsAny<IEnumerable<short>>()))
                .Callback<IEnumerable<short>>(ids => _deletedRewards.AddRange(ids))
                .ReturnsAsync(new List<MinigameRewardDto>());
            rewards.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MinigameRewardDto>>()))
                .Callback<IEnumerable<MinigameRewardDto>>(rows => _savedRewards.AddRange(rows))
                .ReturnsAsync(true);
            var scores = new Mock<IDao<MinigameScoreDto, short>>();
            scores.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<MinigameScoreDto>>()))
                .Callback<IEnumerable<MinigameScoreDto>>(rows => _savedScores.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new MinigameParser(rewards.Object, scores.Object);
        }

        [TestMethod]
        public async Task RewardIdsRunFromZeroWithoutGaps()
        {
            await _parser.InsertMinigamesAsync();

            CollectionAssert.AreEqual(Enumerable.Range(0, _savedRewards.Count).Select(i => (short)i).ToList(),
                _savedRewards.Select(r => r.MinigameRewardId).ToList());
        }

        [TestMethod]
        public async Task EveryMinigameHasARewardForEachOfItsFiveBoxes()
        {
            await _parser.InsertMinigamesAsync();

            foreach (var minigame in _savedRewards.GroupBy(r => r.MinigameVNum))
            {
                CollectionAssert.AreEquivalent(new byte[] { 0, 1, 2, 3, 4 },
                    minigame.Select(r => r.Level).Distinct().ToList(), $"Minigame {minigame.Key}");
            }
        }

        [TestMethod]
        public async Task EachMinigameTypeHasSixRisingScoreBands()
        {
            await _parser.InsertMinigamesAsync();

            foreach (var type in Enum.GetValues<MinigameType>())
            {
                var bands = _savedScores.Where(s => s.MinigameType == type).OrderBy(s => s.Level).ToList();
                CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3, 4, 5 }, bands.Select(b => b.Level).ToList(), type.ToString());
                CollectionAssert.AreEqual(bands.Select(b => b.MaximumScore).OrderBy(s => s).ToList(),
                    bands.Select(b => b.MaximumScore).ToList(), type.ToString());
                CollectionAssert.AreEqual(bands.Select(b => (short)((byte)type * 10 + b.Level)).ToList(),
                    bands.Select(b => b.MinigameScoreId).ToList(), type.ToString());
            }
        }

        [TestMethod]
        public async Task TheQuarrysFirstBoxNeedsAThousandPoints()
        {
            await _parser.InsertMinigamesAsync();

            var quarry = _savedScores.Where(s => s.MinigameType == MinigameType.Quarry).ToDictionary(s => s.Level, s => s.MaximumScore);
            Assert.AreEqual(999, quarry[0]);
            Assert.AreEqual(4999, quarry[1]);
        }

        [TestMethod]
        public async Task ARerunReplacesTheStoredRewards()
        {
            _existingRewards.Add(new MinigameRewardDto { MinigameRewardId = 500 });

            await _parser.InsertMinigamesAsync();

            CollectionAssert.AreEqual(new short[] { 500 }, _deletedRewards);
            Assert.IsTrue(_savedRewards.Count > 0);
        }
    }
}
