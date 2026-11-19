//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Packets.ClientPackets.Bank;
using NosCore.Packets.ClientPackets.Movement;
using NosCore.Packets.Enumerations;
using NosCore.Packets.Interfaces;
using NosCore.Tests.Shared;
using SpecLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Networking
{
    [TestClass]
    public class MapExecutorHandlingTests
    {
        private const int Changes = 200;

        private TestWorld World;
        private ClientSession Session;
        private Func<ClientSession, Task> OnGbox = _ => Task.CompletedTask;
        private Func<ClientSession, Task> OnPulse = _ => Task.CompletedTask;
        private long GoldBefore;
        private bool HeldItsMap;
        private bool MapWorkRanMeanwhile;
        private bool HeldItsMapAgain;
        private MapInstance? MapAfterwards;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync(new List<IPacketHandler>
            {
                new DelegateHandler<GboxPacket>(s => OnGbox(s)),
                new DelegateHandler<PulsePacket>(s => OnPulse(s)),
            });
            Session.LastKeepAliveIdentity = 1;
        }

        [TestMethod]
        public async Task APacketHandlerRunsOnThePlayersMap()
        {
            await new Spec("A packet handler runs on the player's map")
                .Given(TheHandlerNotesWhetherItHoldsThePlayersMap)
                .WhenAsync(APacketArrives)
                .Then(TheHandlerShouldHaveHeldThePlayersMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task GoldChangedByPacketsAndByMapWorkAtOnceKeepsEveryChange()
        {
            await new Spec("Gold changed by packets and by map work at once keeps every change")
                .Given(EachPacketAddsOneGoldAcrossAnAwait)
                .WhenAsync(PacketsAndMapWorkEachAddGoldAtTheSameTime)
                .Then(TheGoldShouldHaveGrownByEveryChange)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHandlerWaitingOutsideItsMapLetsTheMapWork()
        {
            await new Spec("A handler waiting outside its map lets the map work")
                .Given(TheHandlerStepsAwayAndWaitsForMapWork)
                .WhenAsync(APacketArrives)
                .Then(TheMapWorkShouldHaveRunWhileTheHandlerWaited)
                .And(TheHandlerShouldHoldItsMapAgainAfterwards)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task AHandlerThatMovesItsPlayerCarriesOnOnTheNewMap()
        {
            await new Spec("A handler that moves its player carries on on the new map")
                .Given(TheHandlerMovesThePlayerToTheirMiniland)
                .WhenAsync(APacketArrives)
                .Then(ThePlayerShouldBeOnTheMiniland)
                .And(TheHandlerShouldHaveCarriedOnOnTheMiniland)
                .ExecuteAsync();
        }

        private void TheHandlerNotesWhetherItHoldsThePlayersMap() =>
            OnGbox = session =>
            {
                HeldItsMap = session.Character.MapInstance.Executor.IsCurrent;
                return Task.CompletedTask;
            };

        private void EachPacketAddsOneGoldAcrossAnAwait()
        {
            GoldBefore = Session.Character.Gold;
            OnGbox = AddOneGoldAsync;
        }

        private void TheHandlerStepsAwayAndWaitsForMapWork()
        {
            OnGbox = async session =>
            {
                var executor = session.Character.MapInstance.Executor;
                await MapExecutor.StepAwayAsync(async () =>
                {
                    MapWorkRanMeanwhile = await RunsOnAnotherFlowAsync(executor);
                });
                HeldItsMapAgain = executor.IsCurrent;
            };
        }

        private void TheHandlerMovesThePlayerToTheirMiniland()
        {
            OnGbox = async session =>
            {
                await World.MapChangeService.ChangeMapInstanceAsync(session, World.MinilandId, 5, 8);
                MapAfterwards = World.GetMap(World.MinilandId);
                HeldItsMapAgain = MapAfterwards.Executor.IsCurrent;
            };
        }

        private Task APacketArrives() => FromNetwork(new GboxPacket { Header = "gbox", Type = BankActionType.DepositGold, Amount = 1 });

        private async Task PacketsAndMapWorkEachAddGoldAtTheSameTime()
        {
            var executor = Session.Character.MapInstance.Executor;
            var packets = Task.Run(async () =>
            {
                for (var i = 0; i < Changes; i++)
                {
                    await APacketArrives();
                }
            });
            var mapWork = Task.WhenAll(Enumerable.Range(0, Changes)
                .Select(_ => Task.Run(() => executor.RunAsync(() => AddOneGoldAsync(Session)))));
            await Task.WhenAll(packets, mapWork);
        }

        private static async Task AddOneGoldAsync(ClientSession session)
        {
            var gold = session.Character.Gold;
            await Task.Yield();
            session.Character.Gold = gold + 1;
        }

        private static async Task<bool> RunsOnAnotherFlowAsync(MapExecutor executor)
        {
            Task work;
            using (ExecutionContext.SuppressFlow())
            {
                work = Task.Run(() => executor.RunAsync(() => Task.CompletedTask));
            }

            return await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(5))) == work;
        }

        private Task FromNetwork(IPacket packet)
        {
            packet.KeepAliveId = (ushort)((Session.LastKeepAliveIdentity ?? 0) + 1);
            return Session.HandlePacketsAsync(new[] { packet }, true);
        }

        private void TheHandlerShouldHaveHeldThePlayersMap() => Assert.IsTrue(HeldItsMap);

        private void TheGoldShouldHaveGrownByEveryChange() => Assert.AreEqual(GoldBefore + 2 * Changes, Session.Character.Gold);

        private void TheMapWorkShouldHaveRunWhileTheHandlerWaited() => Assert.IsTrue(MapWorkRanMeanwhile);

        private void TheHandlerShouldHoldItsMapAgainAfterwards() => Assert.IsTrue(HeldItsMapAgain);

        private void ThePlayerShouldBeOnTheMiniland() => Assert.AreEqual(World.MinilandId, Session.Character.MapInstance.MapInstanceId);

        private void TheHandlerShouldHaveCarriedOnOnTheMiniland() => Assert.IsTrue(HeldItsMapAgain);

        private sealed class DelegateHandler<TPacket>(Func<ClientSession, Task> handle) : PacketHandler<TPacket>, IWorldPacketHandler
            where TPacket : IPacket
        {
            public override Task ExecuteAsync(TPacket packet, ClientSession clientSession) => handle(clientSession);
        }
    }
}
