//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.EndToEnd.Tests.Infrastructure;
using NosCore.Packets.ClientPackets.Exchanges;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Packets.ServerPackets.Exchanges;
using NosCore.Packets.ServerPackets.Inventory;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;

namespace NosCore.EndToEnd.Tests.Scenarios
{
    [TestClass]
    public class TradeTests
    {
        private static ExchangeRequestPacket Request(RequestExchangeType type, TestClient other) =>
            new() { RequestType = type, VisualId = other.CharacterId };

        private static Func<ServerExcListPacket, bool> Offer(TestClient trader, long gold) =>
            packet => packet.VisualType == VisualType.Player && packet.VisualId == trader.CharacterId
                && packet.Gold == gold && packet.BankGold == 0;

        [TestMethod]
        public async Task GoldOfferedInATradeChangesHands()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: c => c.Gold = 1000);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            await alice.SendAsync(Request(RequestExchangeType.Requested, bob));
            await bob.ExpectAsync<Dlgi2Packet>(p => p.YesPacket is ExchangeRequestPacket { RequestType: RequestExchangeType.List } yes
                && yes.VisualId == alice.CharacterId, "the trade invitation");
            await bob.SendAsync(Request(RequestExchangeType.List, alice));
            await alice.ExpectAsync<ServerExcListPacket>();
            await bob.ExpectAsync<ServerExcListPacket>();

            await alice.SendAsync(new ExcListPacket { Gold = 400, BankGold = 0 });
            await bob.ExpectAsync<ServerExcListPacket>(Offer(alice, 400), "Alice's offer");
            await bob.SendAsync(new ExcListPacket { Gold = 0, BankGold = 0 });
            await alice.ExpectAsync<ServerExcListPacket>(Offer(bob, 0), "Bob's offer");

            await alice.SendAsync(new ExchangeRequestPacket { RequestType = RequestExchangeType.Confirmed });
            await alice.ExpectAsync<InfoiPacket>(p => p.Message == Game18NConstString.TradeWaitingConfirm, "the wait for the other side");
            await bob.SendAsync(new ExchangeRequestPacket { RequestType = RequestExchangeType.Confirmed });

            await alice.ExpectAsync<GoldPacket>(p => p is { Gold: 600, Unknown: 0 }, "Alice's gold");
            await alice.ExpectAsync<ExcClosePacket>(p => p.Type == ExchangeResultType.Success, "the trade closing");
            await bob.ExpectAsync<GoldPacket>(p => p is { Gold: 400, Unknown: 0 }, "Bob's gold");
            await bob.ExpectAsync<ExcClosePacket>(p => p.Type == ExchangeResultType.Success, "the trade closing");
            Assert.AreEqual(600, (await Harness.StoredAsync(alice.CharacterId)).Gold, "a settled trade is stored at once");
            Assert.AreEqual(400, (await Harness.StoredAsync(bob.CharacterId)).Gold);
        }

        [TestMethod]
        public async Task ADeclinedTradeMovesNothing()
        {
            await using var alice = await Harness.EnterAsync("Alice", x: 80, y: 116, configure: c => c.Gold = 1000);
            await using var bob = await Harness.EnterAsync("Bob", x: 81, y: 116);

            var asked = alice.Mark();
            await alice.SendAsync(Request(RequestExchangeType.Requested, bob));
            await bob.ExpectAsync<Dlgi2Packet>();
            await bob.SendAsync(Request(RequestExchangeType.Declined, alice));

            await alice.ExpectAsync<SayiPacket>(p => p.VisualId == alice.CharacterId && p.Type == SayColorType.Yellow
                && p.Message == Game18NConstString.TradeCancelled2, "the cancelled trade");
            Assert.IsTrue(await alice.NoneSinceAsync<ServerExcListPacket>(asked, null, TimeSpan.FromSeconds(1)), "no trade window opens");
            Assert.IsTrue(await alice.NoneSinceAsync<GoldPacket>(asked, null, TimeSpan.Zero), "no gold moves");
        }
    }
}
