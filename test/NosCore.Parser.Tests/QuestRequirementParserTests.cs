//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.StaticEntities;
using NosCore.Database.Entities;
using NosCore.Parser.Parsers;
using NosCore.Tests.Shared;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class QuestRequirementParserTests
    {
        private TestWorld World;
        private string _folder;
        private IDao<QuestDto, short> _quests;

        private static string Quest(short questId, short requiredQuestId, short nextQuestId) =>
            "BEGIN\r\n" +
            $"VNUM\t{questId}\t12\t1\t1\t{requiredQuestId}\t-1\r\n" +
            "LEVEL\t20\t99\r\n" +
            "TITLE\tzts1806e\r\n" +
            "DESC\tzts1807e\r\n" +
            "TALK\t-1\t-1\t-1\t-1\r\n" +
            "TARGET\t-1\t-1\t-1\r\n" +
            "DATA\t305\t-1\t-1\t-1\r\n" +
            "PRIZE\t-1\t-1\t-1\t-1\r\n" +
            $"LINK\t{nextQuestId}\r\n" +
            "END";

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            World.Inject<ILoggerFactory>(NullLoggerFactory.Instance);
            _quests = World.Create<Dao<Quest, QuestDto, short>>();
            World.Inject(_quests);
            World.Inject<IDao<QuestObjectiveDto, Guid>>(World.Create<Dao<QuestObjective, QuestObjectiveDto, Guid>>());
            World.Inject<IDao<QuestQuestRewardDto, Guid>>(World.Create<Dao<QuestQuestReward, QuestQuestRewardDto, Guid>>());
            World.Inject<IDao<QuestRewardDto, short>>(World.Create<Dao<QuestReward, QuestRewardDto, short>>());

            _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_folder);
            await File.WriteAllTextAsync(Path.Combine(_folder, "quest.dat"),
                Quest(5970, 5969, 5972) + "\r\n#=======\r\n" + Quest(5972, 5970, -1));
        }

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_folder, true);

        [TestMethod]
        public async Task AQuestRequiringOneTheFileDoesNotHoldRequiresNothing()
        {
            await World.Create<QuestParser>().ImportQuestsAsync(_folder);

            Assert.IsNull(_quests.LoadAll().Single(q => q.QuestId == 5970).RequiredQuestId);
        }

        [TestMethod]
        public async Task AQuestRequiringOneTheFileHoldsKeepsItsRequirement()
        {
            await World.Create<QuestParser>().ImportQuestsAsync(_folder);

            Assert.AreEqual((short)5970, _quests.LoadAll().Single(q => q.QuestId == 5972).RequiredQuestId);
        }
    }
}
