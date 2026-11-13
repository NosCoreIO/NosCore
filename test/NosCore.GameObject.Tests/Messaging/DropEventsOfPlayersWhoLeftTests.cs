//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Messaging;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.Tests.Shared;
using Moq;
using Wolverine;

namespace NosCore.GameObject.Tests.Messaging
{
    [TestClass]
    public class DropEventsOfPlayersWhoLeftTests
    {
        private TestWorld World;
        private ClientSession _session;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _session = await World.CreateSessionAsync();
        }

        [TestMethod]
        public void AnEventAboutAPlayerInTheWorldIsKept()
        {
            Assert.IsFalse(new NpcTalkedEvent(_session.Character, new Mock<INonPlayableEntity>().Object).PlayerHasLeft);
            Assert.IsFalse(new MapInstanceEnteredEvent(_session, _session.Character.MapInstance).PlayerHasLeft);
        }

        [TestMethod]
        public void AnEventAboutAPlayerWhoLeftIsDropped()
        {
            var character = _session.Character;
            var map = character.MapInstance;

            _session.ClearPlayerEntity();

            Assert.IsTrue(new NpcTalkedEvent(character, new Mock<INonPlayableEntity>().Object).PlayerHasLeft);
            Assert.IsTrue(new ItemWornEvent(character, 1).PlayerHasLeft);
            Assert.IsTrue(new MapInstanceEnteredEvent(_session, map).PlayerHasLeft);
        }

        [TestMethod]
        public async Task TheBusDoesNotHandleAnEventAboutAPlayerWhoLeft()
        {
            var handled = new HandledProbes();
            using var host = await Host.CreateDefaultBuilder()
                .ConfigureServices(s => s.AddSingleton(handled))
                .UseWolverine(opts =>
                {
                    opts.Discovery.DisableConventionalDiscovery().IncludeType<ProbeHandler>();
                    WolverineHostExtensions.ConfigurePolicies(opts);
                })
                .StartAsync();
            var bus = host.Services.GetRequiredService<IMessageBus>();

            await bus.InvokeAsync(new Probe("left", PlayerHasLeft: true));
            await bus.InvokeAsync(new Probe("here", PlayerHasLeft: false));

            CollectionAssert.AreEqual(new List<string> { "here" }, handled.Names);
            await host.StopAsync();
        }

        public sealed record Probe(string Name, bool PlayerHasLeft) : IPlayerEvent;

        public sealed class HandledProbes
        {
            public List<string> Names { get; } = [];
        }

        public sealed class ProbeHandler
        {
            public static void Handle(Probe probe, HandledProbes handled) => handled.Names.Add(probe.Name);
        }
    }
}
