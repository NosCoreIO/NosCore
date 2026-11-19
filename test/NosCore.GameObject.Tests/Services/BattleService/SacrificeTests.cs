//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class SacrificeTests
    {
        private const short CircleCardId = 531;
        private const short SpiritCardId = 546;

        private TestWorld World;
        private ClientSession Caster;
        private ClientSession Ally;
        private BuffService Buffs;
        private readonly List<IPacket> Broadcast = new();
        private readonly List<BCardDto> SkillCards = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Caster = await World.CreateSessionAsync();
            Ally = await World.CreateSessionAsync();
            var mapSessions = new Mock<ISessionGroup>();
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(packets => Broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => Broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            Caster.Character.MapInstance.Sessions = mapSessions.Object;

            var cards = World.Freeze<Mock<ICardCatalog>>();
            foreach (var card in new[]
                     {
                         new CardDto { CardId = CircleCardId, Duration = 300, GroupId = 631 },
                         new CardDto { CardId = SpiritCardId, Duration = 300, GroupId = 631 },
                     })
            {
                cards.Setup(c => c.GetCard(card.CardId)).Returns(card);
                cards.Setup(c => c.GetCardBCards(card.CardId)).Returns(new List<BCardDto>());
            }

            Buffs = World.Create<BuffService>();
        }

        [TestMethod]
        public async Task TheCasterTakesTheSpiritAndTheAllyTheCircle()
        {
            await new Spec("Sacrifice cast on an ally puts the magic circle on the ally and Spirit of Sacrifice on the caster")
                .Given(TheSkillDeclaresTheCircleAtHundredPercent)
                .And(TheSkillDeclaresTheSpiritAt_Percent, 100)
                .WhenAsync(TheCasterCastsItOnTheAlly)
                .Then(TheCasterHasTheSpirit_, true)
                .And(TheCasterHasTheCircle_, false)
                .And(TheAllyHasTheCircle_, true)
                .And(TheAllyHasTheSpirit_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheSpiritIconShowsOnTheCasterAtTheCastersLevel()
        {
            await new Spec("Everyone on the map sees the Spirit of Sacrifice icon on the caster, at the caster's level")
                .Given(TheCasterIsAtLevel_, 57)
                .And(TheSkillDeclaresTheSpiritAt_Percent, 100)
                .WhenAsync(TheCasterCastsItOnTheAlly)
                .Then(TheSpiritIconIsShownOnTheCasterAtLevel_, 57)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AFailedRollGivesTheCasterNothing()
        {
            await new Spec("A roll outside the declared chance gives the caster no Spirit of Sacrifice")
                .Given(TheSkillDeclaresTheSpiritAt_Percent, 60)
                .And(TheRollIs_, 60)
                .WhenAsync(TheCasterCastsItOnTheAlly)
                .Then(TheCasterHasTheSpirit_, false)
                .And(TheAllyHasTheSpirit_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WithoutACasterNobodyTakesTheSpirit()
        {
            await new Spec("Cards inflicted with no caster give nobody Spirit of Sacrifice")
                .Given(TheSkillDeclaresTheSpiritAt_Percent, 100)
                .WhenAsync(TheCardsAreInflictedOnTheAllyWithoutACaster)
                .Then(TheAllyHasTheSpirit_, false)
                .ExecuteAsync();
        }

        private void TheSkillDeclaresTheCircleAtHundredPercent() =>
            SkillCards.Add(Declares(BCardEffect.BuffChanceCausing, 100, CircleCardId));

        private void TheSkillDeclaresTheSpiritAt_Percent(int percent) =>
            SkillCards.Add(Declares(BCardEffect.MeditationSkillSacrifice, percent, SpiritCardId));

        private void TheCasterIsAtLevel_(int level) => Caster.Character.Level = (byte)level;

        private void TheRollIs_(int value) => World.Random.Setup(r => r.Next(0, 100)).Returns(value);

        private Task TheCasterCastsItOnTheAlly() => Buffs.InflictCardsAsync(Ally.Character, Caster.Character, SkillCards);

        private Task TheCardsAreInflictedOnTheAllyWithoutACaster() => Buffs.InflictCardsAsync(Ally.Character, null, SkillCards);

        private void TheCasterHasTheSpirit_(bool expected) => Assert.AreEqual(expected, Buffs.HasBuff(Caster.Character, SpiritCardId));

        private void TheCasterHasTheCircle_(bool expected) => Assert.AreEqual(expected, Buffs.HasBuff(Caster.Character, CircleCardId));

        private void TheAllyHasTheCircle_(bool expected) => Assert.AreEqual(expected, Buffs.HasBuff(Ally.Character, CircleCardId));

        private void TheAllyHasTheSpirit_(bool expected) => Assert.AreEqual(expected, Buffs.HasBuff(Ally.Character, SpiritCardId));

        private void TheSpiritIconIsShownOnTheCasterAtLevel_(int level) =>
            PacketAssert.AreEqual(new BfPacket
            {
                VisualType = VisualType.Player,
                VisualId = Caster.Character.VisualId,
                Buff = new BfPacket.BuffElementSubPacket { ChargeValue = 0, BuffId = SpiritCardId, Duration = 300 },
                BuffLevel = level,
            }, Broadcast.OfType<BfPacket>().Single());

        private static BCardDto Declares(BCardEffect effect, int percent, short cardId) => new()
        {
            Type = effect.Type(),
            SubType = effect.SubType(),
            FirstData = percent,
            SecondData = cardId,
        };
    }
}
