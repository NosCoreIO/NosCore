//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Messaging.Handlers.Battle;
using NosCore.GameObject.Messaging.Handlers.Quest;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Messaging
{
    [TestClass]
    public class GameEventDispatcherTests
    {
        private TestWorld World;
        private ClientSession _session;
        private Handled _handled;
        private GameEventDispatcher _dispatcher;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
            _handled = new Handled();
            var services = new ServiceCollection().AddSingleton(_handled).BuildServiceProvider();
            _dispatcher = new GameEventDispatcher(services, NullLogger<GameEventDispatcher>.Instance,
                new Mock<ILogLanguageLocalizer<LogLanguageKey>>().Object);
        }

        [TestMethod]
        public void AnEventAboutAPlayerInTheWorldIsKept()
        {
            Assert.IsFalse(new NpcTalkedEvent(_session.Character, new Mock<INonPlayableEntity>().Object).PlayerHasLeft);
            Assert.IsFalse(new MapInstanceEnteredEvent(_session, _session.Character.MapInstance, _session.Character.MapInstance).PlayerHasLeft);
        }

        [TestMethod]
        public void AnEventAboutAPlayerWhoLeftIsDropped()
        {
            var character = _session.Character;
            var map = character.MapInstance;

            _session.ClearPlayerEntity();

            Assert.IsTrue(new NpcTalkedEvent(character, new Mock<INonPlayableEntity>().Object).PlayerHasLeft);
            Assert.IsTrue(new ItemWornEvent(character, 1).PlayerHasLeft);
            Assert.IsTrue(new MapInstanceEnteredEvent(_session, map, map).PlayerHasLeft);
        }

        [TestMethod]
        public async Task AnEventAboutAPlayerWhoLeftIsNotHandled()
        {
            await _dispatcher.RaiseAsync(new Probe("left", PlayerHasLeft: true));
            await _dispatcher.RaiseAsync(new Probe("here", PlayerHasLeft: false));

            CollectionAssert.AreEqual(new List<string> { "first here", "second here", "third here" }, _handled.Names);
        }

        [TestMethod]
        public async Task EveryHandlerRunsInItsOrderBeforeTheRaiseReturns()
        {
            await _dispatcher.RaiseAsync(new Probe("raised", PlayerHasLeft: false));

            CollectionAssert.AreEqual(new List<string> { "first raised", "second raised", "third raised" }, _handled.Names);
        }

        [TestMethod]
        public async Task AHandlerThatFailsDoesNotStopTheOthers()
        {
            await _dispatcher.RaiseAsync(new Probe("fail", PlayerHasLeft: false));

            CollectionAssert.AreEqual(new List<string> { "first fail", "third fail" }, _handled.Names);
        }

        [TestMethod]
        public void AKillIsRewardedBeforeItCountsForAQuest()
        {
            var handlers = GameEventDispatcher.HandlerTypesOf<EntityDiedEvent>().ToList();

            var reward = handlers.IndexOf(typeof(RewardDistributionHandler));
            Assert.IsTrue(reward >= 0 && reward < handlers.IndexOf(typeof(OnEntityDiedHandler)));
        }

        public sealed record Probe(string Name, bool PlayerHasLeft) : IGameEvent, IPlayerEvent;

        public sealed class Handled
        {
            public List<string> Names { get; } = [];
        }

        [GameEventOrder(-1)]
        public sealed class ZFirstProbeHandler(Handled handled)
        {
            public void Handle(Probe probe) => handled.Names.Add("first " + probe.Name);
        }

        public sealed class ASecondProbeHandler(Handled handled)
        {
            public async Task HandleAsync(Probe probe)
            {
                await Task.Yield();
                if (probe.Name == "fail")
                {
                    throw new InvalidOperationException();
                }

                handled.Names.Add("second " + probe.Name);
            }
        }

        public sealed class BThirdProbeHandler(Handled handled)
        {
            public void Handle(Probe probe) => handled.Names.Add("third " + probe.Name);
        }
    }
}
