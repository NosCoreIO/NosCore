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
using NodaTime;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.Networking.SessionGroup;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using SpecLight;
using CharacterExperience = NosCore.Algorithm.ExperienceService.ExperienceService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class LowLevelBuffServiceTests
    {
        private const short MartialArtsCardId = 684;
        private static readonly CardDto MartialArts = new()
        {
            CardId = MartialArtsCardId, Duration = 30000, Trigger = CardTrigger.LowLevelCharacter,
        };

        private TestWorld World;
        private ClientSession Session;
        private BuffService Buffs;
        private LowLevelBuffService Service;
        private readonly List<IPacket> Broadcast = new();

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            var mapSessions = new Mock<ISessionGroup>();
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>()))
                .Callback<IPacket[]>(packets => Broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            mapSessions.Setup(s => s.Broadcast(It.IsAny<IPacket[]>(), It.IsAny<ISessionMatcher>()))
                .Callback<IPacket[], ISessionMatcher>((packets, _) => Broadcast.AddRange(packets))
                .Returns(Task.CompletedTask);
            Session.Character.MapInstance.Sessions = mapSessions.Object;

            var cards = World.Freeze<Mock<ICardCatalog>>();
            cards.Setup(c => c.GetTriggeredCard(CardTrigger.LowLevelCharacter)).Returns(MartialArts);
            cards.Setup(c => c.GetCard(MartialArtsCardId)).Returns(MartialArts);
            cards.Setup(c => c.GetCardBCards(MartialArtsCardId)).Returns(new List<BCardDto>
            {
                new() { CardId = MartialArtsCardId, Type = BCardType.CardType.StanceAndExperience, SubType = 41, FirstData = 100 },
            });
            Buffs = World.Create<BuffService>();
            World.Inject<IBuffService>(Buffs);
            Service = World.Create<LowLevelBuffService>();
            World.Inject<ILowLevelBuffService>(Service);
        }

        [TestMethod]
        public async Task ACharacterAtTheLastLevelGetsTheBuff()
        {
            await new Spec("A character up to level 80 is given the buff, shown at the character's own level")
                .Given(TheCharacterIsAtLevel_, 80)
                .WhenAsync(TheBuffIsRefreshed)
                .Then(TheCharacterHasTheBuff_, true)
                .And(TheBuffIconShowsAtLevel_, 80)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterPastTheLastLevelGetsNothing()
        {
            await new Spec("A character past level 80 is not given the buff")
                .Given(TheCharacterIsAtLevel_, 81)
                .WhenAsync(TheBuffIsRefreshed)
                .Then(TheCharacterHasTheBuff_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ADeadCharacterGetsNothing()
        {
            await new Spec("A dead character is not given the buff until revived")
                .Given(TheCharacterIsAtLevel_, 10)
                .And(TheCharacterIsDead)
                .WhenAsync(TheBuffIsRefreshed)
                .Then(TheCharacterHasTheBuff_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ACharacterWithTheBuffIsNotSentItAgain()
        {
            await new Spec("A character that already has the buff is not sent it again")
                .Given(TheCharacterIsAtLevel_, 10)
                .AndAsync(TheCharacterHasBeenGivenTheBuff)
                .WhenAsync(TheBuffIsRefreshed)
                .Then(NoBuffIconIsSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task TheBuffIsGivenAgainWhenItRunsOut()
        {
            await new Spec("The buff is given again as soon as it runs out")
                .Given(TheCharacterIsAtLevel_, 10)
                .AndAsync(TheCharacterHasBeenGivenTheBuff)
                .WhenAsync(TheBuffRunsOut)
                .Then(TheCharacterHasTheBuff_, true)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AnotherCardRunningOutGivesNothing()
        {
            await new Spec("Another card running out does not give the buff")
                .Given(TheCharacterIsAtLevel_, 10)
                .WhenAsync(AnotherCardRunsOut)
                .Then(TheCharacterHasTheBuff_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LevellingPastTheLastLevelTakesTheBuffOff()
        {
            await new Spec("Levelling from 80 to 81 takes the buff off")
                .Given(TheCharacterIsAtLevel_, 80)
                .AndAsync(TheCharacterHasBeenGivenTheBuff)
                .WhenAsync(TheCharacterLevelsUp)
                .Then(TheCharacterHasTheBuff_, false)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LevellingUpBelowTheLastLevelKeepsTheBuff()
        {
            await new Spec("Levelling from 79 to 80 keeps the buff")
                .Given(TheCharacterIsAtLevel_, 79)
                .AndAsync(TheCharacterHasBeenGivenTheBuff)
                .WhenAsync(TheCharacterLevelsUp)
                .Then(TheCharacterHasTheBuff_, true)
                .ExecuteAsync();
        }

        private void TheCharacterIsAtLevel_(int level) => Session.Character.Level = (byte)level;

        private void TheCharacterIsDead() => Session.Character.IsAlive = false;

        private async Task TheCharacterHasBeenGivenTheBuff()
        {
            await Service.RefreshAsync(Session.Character);
            Broadcast.Clear();
        }

        private Task TheBuffIsRefreshed() => Service.RefreshAsync(Session.Character);

        private async Task TheBuffRunsOut()
        {
            World.Clock.Advance(Duration.FromSeconds(3001));
            await Buffs.TickAsync(Session.Character);
            Assert.IsFalse(Buffs.HasBuff(Session.Character, MartialArtsCardId));
            await Service.RenewAsync(Session.Character, MartialArtsCardId);
        }

        private Task AnotherCardRunsOut() => Service.RenewAsync(Session.Character, 685);

        private Task TheCharacterLevelsUp()
        {
            var level = Session.Character.Level;
            Session.Character.LevelXp = new CharacterExperience().GetExperience(level) - 1;
            return World.Create<ExperienceProgressionService>().AddExperienceAsync(Session.Character, 1, 0, 0, 0, 0);
        }

        private void TheCharacterHasTheBuff_(bool expected) =>
            Assert.AreEqual(expected, Buffs.HasBuff(Session.Character, MartialArtsCardId));

        private void TheBuffIconShowsAtLevel_(int level) =>
            PacketAssert.AreEqual(new BfPacket
            {
                VisualType = VisualType.Player,
                VisualId = Session.Character.VisualId,
                Buff = new BfPacket.BuffElementSubPacket { ChargeValue = 0, BuffId = MartialArtsCardId, Duration = 30000 },
                BuffLevel = level,
            }, Broadcast.OfType<BfPacket>().Single());

        private void NoBuffIconIsSent() => Assert.IsFalse(Broadcast.OfType<BfPacket>().Any());
    }
}
