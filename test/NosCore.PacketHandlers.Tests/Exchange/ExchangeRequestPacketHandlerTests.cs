//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.BlacklistHub;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ExchangeService;
using NosCore.PacketHandlers.Exchange;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Exchange
{
    [TestClass]
    public class ExchangeRequestPacketHandlerTests
    {
        private ExchangeRequestPackettHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IExchangeService> ExchangeService = null!;
        private Mock<IBlacklistHub> BlacklistHub = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            ExchangeService = new Mock<IExchangeService>();
            BlacklistHub = new Mock<IBlacklistHub>();
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(It.IsAny<long>()))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>()));

            Handler = new ExchangeRequestPackettHandler(
                ExchangeService.Object,
                Logger,
                BlacklistHub.Object,
                TestHelpers.Instance.LogLanguageLocalizer,
                TestHelpers.Instance.SessionRegistry);
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
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
        }

        private void TargetIsAlreadyInExchange()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            ExchangeService.Setup(x => x.CheckExchange(It.IsAny<long>())).Returns(true);
        }

        private void TargetBlocksExchange()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            TargetSession.Character.ExchangeBlocked = true;
            ExchangeService.Setup(x => x.CheckExchange(It.IsAny<long>())).Returns(false);
        }

        private void TargetIsBlacklisted()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            ExchangeService.Setup(x => x.CheckExchange(It.IsAny<long>())).Returns(false);
            BlacklistHub.Setup(x => x.GetBlacklistedAsync(Session.Character.VisualId))
                .Returns(Task.FromResult(new List<CharacterRelationStatus>
                {
                    new() { CharacterId = TargetSession.Character.VisualId }
                }));
        }

        private void ExchangeIsOpen()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            TargetSession.Character.MapInstance = Session.Character.MapInstance;
            ExchangeService.Setup(x => x.GetTargetId(It.IsAny<long>()))
                .Returns((long?)TargetSession.Character.CharacterId);
            ExchangeService.Setup(x => x.CloseExchange(It.IsAny<long>(), It.IsAny<ExchangeResultType>()))
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
            ExchangeService.Verify(x => x.OpenExchange(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
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

        private void ShouldReceiveBlacklistedMessage()
        {
            var packet = Session.LastPackets.FirstOrDefault(s => s is SayiPacket) as SayiPacket;
            Assert.IsNotNull(packet);
            Assert.AreEqual(Game18NConstString.AlreadyBlacklisted, packet.Message);
        }

        private void ExchangeShouldBeClosed()
        {
            ExchangeService.Verify(x => x.CloseExchange(It.IsAny<long>(), ExchangeResultType.Failure), Times.Once);
        }
    }
}
