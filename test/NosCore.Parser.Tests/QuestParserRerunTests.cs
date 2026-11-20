//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.Dao;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using NosCore.Parser.Parsers;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class QuestParserRerunTests
    {
        private const string QuestFile =
            "BEGIN\r\n" +
            "VNUM\t1510\t13\t0\t-1\t-1\t-1\r\n" +
            "LEVEL\t1\t99\r\n" +
            "TITLE\tzts1e\r\n" +
            "DESC\tzts2e\r\n" +
            "TALK\t-1\t-1\t0\t0\r\n" +
            "TARGET\t-1\t-1\t-1\r\n" +
            "DATA\t350\t-1\t-1\t-1\r\n" +
            "DATA\t25\t5\t5\t-1\r\n" +
            "PRIZE\t4\t6\t-1\t-1\r\n" +
            "LINK\t-1\r\n" +
            "END";

        private TestWorld World;
        private string _folder;
        private IDao<QuestObjectiveDto, Guid> _objectives;
        private IDao<QuestQuestRewardDto, Guid> _questRewards;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject<ILoggerFactory>(NullLoggerFactory.Instance);
            World.Inject<IDao<QuestDto, short>>(World.Create<Dao<Quest, QuestDto, short>>());
            _objectives = World.Create<Dao<QuestObjective, QuestObjectiveDto, Guid>>();
            World.Inject(_objectives);
            _questRewards = World.Create<Dao<QuestQuestReward, QuestQuestRewardDto, Guid>>();
            World.Inject(_questRewards);
            var rewards = World.Create<Dao<QuestReward, QuestRewardDto, short>>();
            World.Inject<IDao<QuestRewardDto, short>>(rewards);
            await rewards.TryInsertOrUpdateAsync(new[] { new QuestRewardDto { QuestRewardId = 4 }, new QuestRewardDto { QuestRewardId = 6 } }).OrThrowAsync();

            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
            await File.WriteAllTextAsync(Path.Combine(_folder, "quest.dat"), QuestFile);
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        private Task ImportAsync() => World.Create<QuestParser>().ImportQuestsAsync(_folder);

        private List<string> Objectives() => _objectives.LoadAll()
            .Select(o => $"{o.QuestObjectiveId} {o.QuestId} {o.FirstData} {o.SecondData} {o.ThirdData} {o.FourthData}").Order().ToList();

        private List<string> Prizes() => _questRewards.LoadAll().Select(l => $"{l.Id} {l.QuestId} {l.QuestRewardId}").Order().ToList();

        [TestMethod]
        public async Task ASecondImportKeepsOneRowPerObjectiveAndPrize()
        {
            await ImportAsync();
            var objectives = Objectives();
            var prizes = Prizes();

            await ImportAsync();

            Assert.AreEqual(2, objectives.Count);
            Assert.AreEqual(2, prizes.Count);
            CollectionAssert.AreEqual(objectives, Objectives());
            CollectionAssert.AreEqual(prizes, Prizes());
        }
    }
}
