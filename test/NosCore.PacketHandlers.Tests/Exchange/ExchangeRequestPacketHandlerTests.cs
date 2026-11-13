//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Ecs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.GameObject.Services.TransferService;
using NosCore.PacketHandlers.Exchange;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Microsoft.Extensions.Logging;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Exchange
{
    [TestClass]
    public class ExchangeRequestPacketHandlerTests
    {
        private TestWorld World;
        private ExchangeRequestPackettHandler Handler;
        private ClientSession Session;
        private ClientSession TargetSession;
        private Mock<IExchangeService> ExchangeService;
        private Mock<IBlacklistHub> BlacklistHub;
        private readonly ILogger<ExchangeRequestPackettHandler> Logger = new Mock<ILogger<ExchangeRequestPackettHandler>>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            Session = await World.CreateSessionAsync();
            TargetSession = await World.CreateSessionAsync();
            ExchangeService = World.Freeze<Mock<IExchangeService>>();
            BlacklistHub = World.Freeze<Mock<IBlacklistHub>>();
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>()));

            Handler = World.Create<ExchangeRequestPackettHandler>();
        }

        [TestMethod]
        public async Task RequestingExchangeWhileInShopShouldBeIgnored()
        {
            await new Spec("Requesting exchange while in shop should be ignored")
                .Given(CharacterIsInShop)
                .WhenAsync(RequestingExchange)
                .Then(NoExchangeShouldBeOpened)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingExchangeWhenTargetAlreadyInExchangeShouldFail()
        {
            await new Spec("Requesting exchange when target already in exchange should fail")
                .Given(TargetIsAlreadyInExchange)
                .WhenAsync(RequestingExchange)
                .Then(ShouldReceiveTradingWithSomeoneMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingExchangeWhenTargetBlocksExchangeShouldFail()
        {
            await new Spec("Requesting exchange when target blocks exchange should fail")
                .Given(TargetBlocksExchange)
                .WhenAsync(RequestingExchange)
                .Then(ShouldReceiveBlockingTradesMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingExchangeWithSelfShouldFail()
        {
            await new Spec("Requesting exchange with self should fail")
                .Given(TargetIsSelf)
                .WhenAsync(RequestingExchangeWithSelf)
                .Then(ShouldReceiveBlockingTradesMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingExchangeWhenTargetAllowsExchangeShouldInviteTarget()
        {
            await new Spec("Requesting exchange when target allows exchange should invite the target")
                .Given(TargetAllowsExchange)
                .WhenAsync(RequestingExchange)
                .Then(TargetShouldBeAskedToTrade)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingExchangeWithSelfShouldNotOpenExchange()
        {
            await new Spec("Requesting exchange with self should not open an exchange")
                .Given(TargetIsSelf)
                .WhenAsync(RequestingExchangeWithSelf)
                .Then(NoExchangeShouldBeOpened)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestingExchangeWithBlacklistedPlayerShouldFail()
        {
            await new Spec("Requesting exchange with blacklisted player should fail")
                .Given(TargetIsBlacklisted)
                .WhenAsync(RequestingExchange)
                .Then(ShouldReceiveBlacklistedMessage)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task CancellingExchangeShouldCloseExchange()
        {
            await new Spec("Cancelling exchange should close exchange")
                .Given(ExchangeIsOpen)
                .WhenAsync(CancellingExchange)
                .Then(ExchangeShouldBeClosed)
                .ExecuteAsync();
        }

        private void CharacterIsInShop()
        {
            Session.Character.InShop = true;
            Session.Character.MapInstance = World.GetBaseMap(1);
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
        }

        private void TargetIsAlreadyInExchange()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            TargetSession.Character.Exchange = ExchangeData.Pair(TargetSession.Character.VisualId, 0).Initiator;
        }

        private void TargetBlocksExchange()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            TargetSession.Character.ExchangeBlocked = true;
        }

        private void TargetAllowsExchange()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            TargetSession.Character.ExchangeBlocked = false;
        }

        private void TargetIsSelf()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
        }

        private void TargetIsBlacklisted()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            var visualId = Session.Character.VisualId;
            var targetVisualId = TargetSession.Character.VisualId;
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(visualId))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new() { CharacterId = targetVisualId }
                }));
        }

        private void ExchangeIsOpen()
        {
            Session.Character.MapInstance = World.GetBaseMap(1);
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            var (own, partner) = ExchangeData.Pair(Session.Character.VisualId, TargetSession.Character.VisualId);
            Session.Character.Exchange = own;
            TargetSession.Character.Exchange = partner;
            ExchangeService.Setup(x => x.CancelExchange(It.IsAny<PlayerComponentBundle>()))
                .Returns(new ExcClosePacket { Type = ExchangeResultType.Failure });
        }

        private async Task RequestingExchange()
        {
            await Handler.ExecuteAsync(new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.Requested,
                VisualId = TargetSession.Character.VisualId
            }, Session);
        }

        private async Task RequestingExchangeWithSelf()
        {
            await Handler.ExecuteAsync(new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.Requested,
                VisualId = Session.Character.VisualId
            }, Session);
        }

        private async Task CancellingExchange()
        {
            await Handler.ExecuteAsync(new ExchangeRequestPacket
            {
                RequestType = RequestExchangeType.Cancelled,
                VisualId = 0
            }, Session);
        }

        private void NoExchangeShouldBeOpened()
        {
            ExchangeService.Verify(x => x.OpenExchange(It.IsAny<PlayerComponentBundle>(), It.IsAny<PlayerComponentBundle>()), Times.Never);
        }

        private void ShouldReceiveTradingWithSomeoneMessage()
        {
            var packet = Session.LastPackets.FirstOrDefault(s => s is Infoi2Packet) as Infoi2Packet;
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.TradingWithSomeoneElse, packet.Message);
        }

        private void ShouldReceiveBlockingTradesMessage()
        {
            var packet = Session.LastPackets.FirstOrDefault(s => s is Infoi2Packet) as Infoi2Packet;
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.BlockingTrades, packet.Message);
        }

        private void TargetShouldBeAskedToTrade()
        {
            var packet = TargetSession.LastPackets.OfType<Dlgi2Packet>().FirstOrDefault();
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.WantAcceptTrade, packet.Question);
        }

        private void ShouldReceiveBlacklistedMessage()
        {
            var packet = Session.LastPackets.FirstOrDefault(s => s is SayiPacket) as SayiPacket;
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.AlreadyBlacklisted, packet.Message);
        }

        private void ExchangeShouldBeClosed()
        {
            ExchangeService.Verify(x => x.CancelExchange(It.IsAny<PlayerComponentBundle>()), Times.Once);
        }
    }
}
