//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Interfaces;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Parsers;
using NosCore.Shared.I18N;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class InstantCombatParserTests
    {
        private readonly List<InstantCombatMonsterDto> _existingMonsters = new();
        private readonly List<InstantCombatDropDto> _existingDrops = new();
        private readonly List<InstantCombatMonsterDto> _savedMonsters = new();
        private readonly List<InstantCombatDropDto> _savedDrops = new();
        private readonly List<short> _deletedMonsters = new();
        private readonly List<short> _deletedDrops = new();
        private InstantCombatParser _parser;

        [TestInitialize]
        public void Setup()
        {
            var monsters = new Mock<IDao<InstantCombatMonsterDto, short>>();
            monsters.Setup(s => s.LoadAll()).Returns(_existingMonsters);
            monsters.Setup(s => s.TryDeleteAsync(It.IsAny<IEnumerable<short>>()))
                .Callback<IEnumerable<short>>(ids => _deletedMonsters.AddRange(ids))
                .ReturnsAsync(new List<InstantCombatMonsterDto>());
            monsters.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<InstantCombatMonsterDto>>()))
                .Callback<IEnumerable<InstantCombatMonsterDto>>(rows => _savedMonsters.AddRange(rows))
                .ReturnsAsync(true);
            var drops = new Mock<IDao<InstantCombatDropDto, short>>();
            drops.Setup(s => s.LoadAll()).Returns(_existingDrops);
            drops.Setup(s => s.TryDeleteAsync(It.IsAny<IEnumerable<short>>()))
                .Callback<IEnumerable<short>>(ids => _deletedDrops.AddRange(ids))
                .ReturnsAsync(new List<InstantCombatDropDto>());
            drops.Setup(s => s.TryInsertOrUpdateAsync(It.IsAny<IEnumerable<InstantCombatDropDto>>()))
                .Callback<IEnumerable<InstantCombatDropDto>>(rows => _savedDrops.AddRange(rows))
                .ReturnsAsync(true);
            _parser = new InstantCombatParser(monsters.Object, drops.Object, NullLogger<InstantCombatParser>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public async Task EveryWaveRowIsStoredInOrderWithIdsFromOne()
        {
            await _parser.InsertInstantCombatWavesAsync();

            Assert.AreEqual(InstantCombatWaves.Monsters.Count, _savedMonsters.Count);
            Assert.AreEqual(InstantCombatWaves.Drops.Count, _savedDrops.Count);
            CollectionAssert.AreEqual(Enumerable.Range(1, _savedMonsters.Count).Select(i => (short)i).ToList(),
                _savedMonsters.Select(m => m.InstantCombatMonsterId).ToList());
            CollectionAssert.AreEqual(Enumerable.Range(1, _savedDrops.Count).Select(i => (short)i).ToList(),
                _savedDrops.Select(d => d.InstantCombatDropId).ToList());
        }

        [TestMethod]
        public async Task ARowKeepsEveryColumnOfItsTable()
        {
            await _parser.InsertInstantCombatWavesAsync();

            var monster = InstantCombatWaves.Monsters[0];
            var savedMonster = _savedMonsters[0];
            Assert.AreEqual(monster, (savedMonster.LevelMinimum, savedMonster.Wave, savedMonster.NpcMonsterVNum,
                savedMonster.Amount, savedMonster.IsMoving));
            var drop = InstantCombatWaves.Drops[0];
            var savedDrop = _savedDrops[0];
            Assert.AreEqual(drop, (savedDrop.LevelMinimum, savedDrop.Wave, savedDrop.ItemVNum, savedDrop.Amount));
        }

        [TestMethod]
        public async Task ARerunReplacesTheStoredRows()
        {
            _existingMonsters.Add(new InstantCombatMonsterDto { InstantCombatMonsterId = 900 });
            _existingDrops.Add(new InstantCombatDropDto { InstantCombatDropId = 901 });

            await _parser.InsertInstantCombatWavesAsync();

            CollectionAssert.AreEqual(new short[] { 900 }, _deletedMonsters);
            CollectionAssert.AreEqual(new short[] { 901 }, _deletedDrops);
            Assert.AreEqual(InstantCombatWaves.Monsters.Count, _savedMonsters.Count);
        }
    }
}
