//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.GameObject.Services.RaidService;
using NosCore.GameObject.Services.MapItemGenerationService;
using NosCore.GameObject.Services.ScriptedInstanceService;
using NosCore.Instances.Abstractions;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.GameObject.Tests.Services.ScriptedInstanceService
{
    [TestClass]
    public class InstanceExitServiceTests
    {
        private TestWorld World;
        private ClientSession Session;
        private Mock<IScriptedInstanceService> ScriptedInstances;
        private Mock<IRaidService> Raids;
        private Mock<IInstanceRunHost> Host;
        private ScriptedInstanceRun Run;
        private InstanceExitService Service;

        private sealed class EmptyScript : InstanceScript;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            ScriptedInstances = World.Freeze<Mock<IScriptedInstanceService>>();
            Raids = World.Freeze<Mock<IRaidService>>();
            Session = await World.CreateSessionAsync();
            Host = new Mock<IInstanceRunHost>();
            Run = new ScriptedInstanceRun(Host.Object, new EmptyScript(),
                new Dictionary<int, MapInstance> { [1] = Session.Character.MapInstance },
                new Dictionary<short, NpcMonsterDto>(), new RandomProvider(), World.Clock, World.SessionRegistry,
                World.Create<AggroService>(), World.Create<ItemThrowService>(), NullLogger.Instance, World.LogLanguageLocalizer, TimeSpan.FromSeconds(30));
            Service = World.Create<InstanceExitService>();
        }

        [TestMethod]
        public async Task LeavingATimeSpaceReturnsThroughItsHost()
        {
            await new Spec("Leaving a time-space sends the player back the way its host returns them")
                .Given(ThePlayerStandsInTheRun)
                .WhenAsync(LeavingAsync)
                .Then(TheHostShouldReturnThePlayer)
                .And(TheRaidShouldNotBeLeft)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingTheTeamsRaidLeavesTheRaid()
        {
            await new Spec("Leaving the raid of the player's own team leaves that raid")
                .Given(ThePlayerStandsInTheRun)
                .And(TheRunIsTheTeamsRaid)
                .WhenAsync(LeavingAsync)
                .Then(TheRaidShouldBeLeft)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task LeavingOutsideAnInstanceDoesNothing()
        {
            await new Spec("Leaving outside a scripted instance does nothing")
                .WhenAsync(LeavingAsync)
                .Then(TheRaidShouldNotBeLeft)
                .And(TheHostShouldNotReturnThePlayer)
                .ExecuteAsync();
        }

        private void ThePlayerStandsInTheRun()
        {
            var roomId = Session.Character.MapInstance.MapInstanceId;
            ScriptedInstances.Setup(s => s.GetRun(roomId)).Returns(Run);
        }

        private void TheRunIsTheTeamsRaid()
        {
            Session.Character.Group.RaidRun = Run;
        }

        private Task LeavingAsync() => Service.LeaveAsync(Session);

        private void TheHostShouldReturnThePlayer() => Host.Verify(h => h.ReturnAsync(Session), Times.Once);

        private void TheHostShouldNotReturnThePlayer() => Host.Verify(h => h.ReturnAsync(It.IsAny<ClientSession>()), Times.Never);

        private void TheRaidShouldBeLeft() => Raids.Verify(r => r.LeaveAsync(Session), Times.Once);

        private void TheRaidShouldNotBeLeft() => Raids.Verify(r => r.LeaveAsync(It.IsAny<ClientSession>()), Times.Never);
    }
}
