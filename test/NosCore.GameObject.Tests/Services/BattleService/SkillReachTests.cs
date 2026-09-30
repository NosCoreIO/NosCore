//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SkillReachTests
    {
        private static SkillInfo Skill(byte range, byte targetType) => new(
            SkillVnum: 1, CastId: 1, Cooldown: 0, AttackAnimation: 0, CastEffect: 0, Effect: 0,
            Type: 0, HitType: TargetHitType.SingleTargetHit, Range: range, TargetRange: 0, TargetType: targetType,
            Element: 0, Duration: 0, MpCost: 0, BCards: Array.Empty<BCardDto>());

        [DataTestMethod]
        [DataRow(1, 1, true)]
        [DataRow(1, 2, true)]
        [DataRow(1, 3, false)]
        [DataRow(7, 8, true)]
        [DataRow(7, 9, false)]
        public void ATargetedSkillReachesItsRangePlusOneCell(int range, int distance, bool inReach)
        {
            Assert.AreEqual(inReach, SkillReach.IsInReach(Skill((byte)range, targetType: 0), distance));
        }

        [TestMethod]
        public void ARangeOfZeroIsNotGated()
        {
            Assert.IsTrue(SkillReach.IsInReach(Skill(range: 0, targetType: 0), distance: 50));
        }

        [TestMethod]
        public void ACasterCentredSkillIsNotGated()
        {
            Assert.IsTrue(SkillReach.IsInReach(Skill(range: 2, targetType: 1), distance: 50));
        }

        [DataTestMethod]
        [DataRow(0, 10, true)]
        [DataRow(5, 10, true)]
        [DataRow(6, 10, true)]
        [DataRow(7, 10, false)]
        [DataRow(1, 1, true)]
        [DataRow(2, 1, false)]
        [DataRow(2, 2, true)]
        [DataRow(3, 2, false)]
        public void AStepIsHalfTheSpeedPlusTheCellLeftBehind(int distance, int speed, bool withinOneStep)
        {
            Assert.AreEqual(withinOneStep, SkillReach.IsWithinOneStep(distance, (byte)speed));
        }
    }
}
