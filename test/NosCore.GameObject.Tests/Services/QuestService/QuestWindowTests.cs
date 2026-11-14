//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.QuestService;
using NosCore.Packets.Enumerations;
using System.Collections.Generic;

namespace NosCore.GameObject.Tests.Services.QuestService
{
    [TestClass]
    public class QuestWindowTests
    {
        [TestMethod]
        public void AMainQuestShowsTheFirstQuestOfItsLine() =>
            Assert.AreEqual((short)2100, QuestOf(2123, line: 2100, secondary: false).GenerateQuestSubPacket(false).QuestLineId);

        [TestMethod]
        public void ASecondaryQuestShowsNoLine() =>
            Assert.AreEqual((short)0, QuestOf(5386, line: 5380, secondary: true).GenerateQuestSubPacket(false).QuestLineId);

        [TestMethod]
        public void MainAndSecondaryQuestsGetTheirOwnSlots()
        {
            var main = QuestOf(2102, line: 2100, secondary: false);
            main.Slot = CharacterQuest.FreeSlot([], secondary: false);
            var secondary = QuestOf(5006, line: 5006, secondary: true);
            secondary.Slot = CharacterQuest.FreeSlot([main], secondary: true);
            var another = QuestOf(5050, line: 5050, secondary: true);
            another.Slot = CharacterQuest.FreeSlot([main, secondary], secondary: true);

            CollectionAssert.AreEqual(new byte[] { 0, 5, 6 }, new[] { main.Slot, secondary.Slot, another.Slot });
            Assert.AreEqual((byte)5, secondary.GenerateQuestSubPacket(false).QuestSlot);
        }

        [TestMethod]
        public void ADoneQuestFreesItsSlot()
        {
            var done = QuestOf(5006, line: 5006, secondary: true);
            done.Slot = 5;
            done.CompletedOn = NodaTime.Instant.FromUnixTimeSeconds(0);

            Assert.AreEqual((byte)5, CharacterQuest.FreeSlot([done], secondary: true));
        }

        [TestMethod]
        public void ASecondaryQuestTakesAMainSlotWhenTheSecondaryOnesAreFull()
        {
            var full = new List<CharacterQuest>();
            for (byte slot = 5; slot < 10; slot++)
            {
                var quest = QuestOf((short)(5000 + slot), line: 5000, secondary: true);
                quest.Slot = slot;
                full.Add(quest);
            }

            Assert.AreEqual((byte)0, CharacterQuest.FreeSlot(full, secondary: true));
        }

        private static CharacterQuest QuestOf(short id, short line, bool secondary) => new()
        {
            QuestId = id,
            Quest = new Quest
            {
                QuestId = id,
                QuestLineId = line,
                IsSecondary = secondary,
                QuestType = QuestType.Hunt,
                QuestObjectives = new List<QuestObjectiveDto>(),
            },
        };
    }
}
