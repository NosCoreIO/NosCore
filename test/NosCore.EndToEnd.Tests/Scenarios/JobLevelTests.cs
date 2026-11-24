//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Database.Entities;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.Quicklist;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Packets.ServerPackets.Visibility;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class JobLevelTests
    {
        private static readonly JobExperienceService JobLevels = new();

        // One kill away from the job level that teaches the taunt.
        private static readonly Action<Character> AboutToLearnTheTaunt = c =>
        {
            c.Class = CharacterClassType.Swordsman;
            c.JobLevel = WorldData.TauntSkillJobLevel - 1;
            c.JobLevelXp = JobLevels.GetJobExperience(CharacterClassType.Swordsman, WorldData.TauntSkillJobLevel - 1) - 1;
        };

        [TestMethod]
        public async Task AJobLevelThatTeachesASkillSaysSoInTheChatAndAcrossTheScreenThenRedrawsTheBar()
        {
            var player = await Harness.AddPlayerAsync("Alice", WorldData.Meadow, 30, 71, AboutToLearnTheTaunt);
            await Harness.TeachAsync(player, WorldData.SwordsmanSwing);
            await using var alice = Harness.Client(player);
            await alice.EnterWorldAsync(player.Slot);
            await alice.ExpectSinceAsync<InPacket>(0, p => p.VisualType == VisualType.Monster && p.VisualId == WorldData.Prey,
                "the prey on the map");

            await Steps.KillAsync(alice, WorldData.Prey);

            await alice.ExpectAsync<MsgiPacket>(p => p.Message == Game18NConstString.JobLevelIncreased, "the job level");
            await alice.ExpectAsync<SayiPacket>(p => p.VisualType == VisualType.Player && p.VisualId == alice.CharacterId
                && p.Type == SayColorType.Yellow && p.Message == Game18NConstString.SkillLearned, "the chat line");
            await alice.ExpectAsync<MsgiPacket>(p => p.Type == MessageType.Default && p.Message == Game18NConstString.SkillLearned,
                "the line across the screen");
            await alice.ExpectAsync<SkiPacket>(p => p.SkillVnums?.Contains(WorldData.TauntSkill) == true, "the skill list with the taunt");
            await alice.ExpectAsync<QSlotPacket>(p => p.Slot == 0, "the first quick bar");
            await alice.ExpectAsync<QSlotPacket>(p => p.Slot == 1, "the second quick bar");
            await alice.ExpectAsync<LevPacket>(p => p.JobLevel == WorldData.TauntSkillJobLevel, "the levels");
        }
    }
}
