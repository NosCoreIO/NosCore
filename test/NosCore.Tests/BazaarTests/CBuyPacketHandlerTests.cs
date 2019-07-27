using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;
using System;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CBuyPacketHandlerTest
    {

        private CBuyPacketHandler _cbuyPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private Mock<IItemProvider> _itemProvider;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            _cbuyPacketHandler = new CBuyPacketHandler(_bazaarHttpClient.Object, _itemProvider.Object, _logger);
        }

        [TestMethod]
        public void BuyWhenExchangeOrTrade()
        {
            throw new NotImplementedException();
            //return
        }

        [TestMethod]
        public void BuyWhenNoItemFound()
        {
            throw new NotImplementedException();
            //            clientSession.SendPacket(new ModalPacket
            //            {
            //                Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                Type = 1
            //            });
        }

        [TestMethod]
        public void BuyWhenDifferentSeller()
        {
            throw new NotImplementedException();
            //            clientSession.SendPacket(new ModalPacket
            //            {
            //                Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                Type = 1
            //            });
        }

        [TestMethod]
        public void BuyWhenDifferentPrice()
        {
            throw new NotImplementedException();
            //            clientSession.SendPacket(new ModalPacket
            //            {
            //                Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                Type = 1
            //            });
        }

        [TestMethod]
        public void BuyWhenCanNotAddItem()
        {
            throw new NotImplementedException();
            //                    clientSession.SendPacket(new InfoPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
            //                            clientSession.Account.Language)
            //                    });
        }

        [TestMethod]
        public void BuyMoreThanSelling()
        {
            throw new NotImplementedException();
            //            clientSession.SendPacket(new ModalPacket
            //            {
            //                Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                Type = 1
            //            });
        }

        [TestMethod]
        public void BuyNotEnoughMoney()
        {
            throw new NotImplementedException();
            //clientSession.SendPacket(new ModalPacket
            //                        {
            //                            Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_MONEY, clientSession.Account.Language),
            //                            Type = 1
            //                        });
        }

        [TestMethod]
        public void Buy()
        {
            throw new NotImplementedException();
            //                            clientSession.SendPacket(clientSession.Character.GenerateSay(
            //                                $"{Language.Instance.GetMessageFromKey(LanguageKey.ITEM_ACQUIRED, clientSession.Account.Language)}: {item.Item.Name[clientSession.Account.Language]} x {packet.Amount}"
            //                                , SayColorType.Yellow
            //                            ));

        }

    }
}
