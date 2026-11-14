//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class DebuffResistanceServiceTests
    {
        private TestWorld World;
        private IAliveEntity Player;
        private Mock<IEquipmentStatsService> Equipment;

        private static readonly CardDto Blackout = new()
        {
            CardId = 7, GroupId = 4, Level = 1, BuffType = BuffType.Bad, Kind = CardKind.General
        };

        private static readonly CardDto DeadlyBlackout = new()
        {
            CardId = 66, GroupId = 4, Level = 3, BuffType = BuffType.Bad, Kind = CardKind.General
        };

        private static readonly CardDto Freeze = new()
        {
            CardId = 30, GroupId = 9, Level = 1, BuffType = BuffType.Bad, Kind = CardKind.Magic
        };

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Player = (await World.CreateSessionAsync()).Character;
            Equipment = World.Freeze<Mock<IEquipmentStatsService>>();
            Wearing();
        }

        private void Wearing(params BCardDto[] bCards) =>
            Equipment.Setup(e => e.Resolve(It.IsAny<IAliveEntity>()))
                .Returns(EquipmentStats.None with { BCards = bCards });

        private static BCardDto Card(BCardEffect effect, int first, int second) => new()
        {
            Type = effect.Type(), SubType = effect.SubType(), FirstData = first, SecondData = second
        };

        private int Modifier(CardDto card, params BuffInstance[] buffs) =>
            World.Create<DebuffResistanceService>().ChanceModifier(Player, card, buffs);

        [TestMethod]
        public void AHatAgainstEveryBadEffectCoversCardsUpToItsLevel()
        {
            Wearing(Card(BCardEffect.DebuffResistanceNeverBadEffectChance, 2, 20));

            Assert.AreEqual(-20, Modifier(Blackout));
            Assert.AreEqual(0, Modifier(DeadlyBlackout));
        }

        [TestMethod]
        public void AResistanceToOneKindIgnoresTheOthers()
        {
            Wearing(Card(BCardEffect.DebuffResistanceNeverBadGeneralEffectChance, 2, 10));

            Assert.AreEqual(-10, Modifier(Blackout));
            Assert.AreEqual(0, Modifier(Freeze));
        }

        [TestMethod]
        public void AnIncreaseCancelsAResistance()
        {
            Wearing(
                Card(BCardEffect.DebuffResistanceNeverBadMagicEffectChance, 5, 30),
                Card(BCardEffect.DebuffResistanceIncreaseBadEffectChance, 5, 10));

            Assert.AreEqual(-20, Modifier(Freeze));
        }

        [TestMethod]
        public void AFamilyProtectionCoversItsFamilyUpToItsLevel()
        {
            Wearing(Card(BCardEffect.BuffPreventingBadEffect, 4, 1));

            Assert.AreEqual(-80, Modifier(Blackout));
            Assert.AreEqual(0, Modifier(DeadlyBlackout));
            Assert.AreEqual(0, Modifier(Freeze));
        }

        [TestMethod]
        public void AResistanceToANamedEffectCoversWeakerCardsOfItsFamily()
        {
            World.Freeze<Mock<ICardCatalog>>().Setup(c => c.GetCard(DeadlyBlackout.CardId)).Returns(DeadlyBlackout);
            Wearing(Card(BCardEffect.SpecialisationBuffResistanceResistanceToEffect, 20, DeadlyBlackout.CardId));

            Assert.AreEqual(-20, Modifier(Blackout));
            Assert.AreEqual(-20, Modifier(DeadlyBlackout));
            Assert.AreEqual(0, Modifier(Freeze));
        }

        [TestMethod]
        public void AResistanceToANamedEffectDoesNotCoverStrongerCards()
        {
            World.Freeze<Mock<ICardCatalog>>().Setup(c => c.GetCard(Blackout.CardId)).Returns(Blackout);
            Wearing(Card(BCardEffect.SpecialisationBuffResistanceResistanceToEffect, 20, Blackout.CardId));

            Assert.AreEqual(-20, Modifier(Blackout));
            Assert.AreEqual(0, Modifier(DeadlyBlackout));
        }

        [TestMethod]
        public void AnActiveBuffAddsItsResistance()
        {
            var buff = new BuffInstance(4222, BuffType.Good, null, Instant.MinValue, Instant.MaxValue,
                new[] { Card(BCardEffect.DebuffResistanceNeverBadEffectChance, 4, 30) });

            Assert.AreEqual(-30, Modifier(Freeze, buff));
        }

        [TestMethod]
        public void AGoodCardIsNeverResisted()
        {
            Wearing(Card(BCardEffect.DebuffResistanceNeverBadEffectChance, 5, 100));

            Assert.AreEqual(0, Modifier(new CardDto { CardId = 8, Level = 1, BuffType = BuffType.Good, Kind = CardKind.General }));
        }
    }
}
