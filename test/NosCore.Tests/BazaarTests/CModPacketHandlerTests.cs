using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.I18N;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using Serilog;
using System;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CModPacketHandlerTest
    {
        private CModPacketHandler _cmodPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            _cmodPacketHandler = new CModPacketHandler(_bazaarHttpClient.Object, _logger);
        }

        [TestMethod]
        public void ModifyWhenInExchange()
        {
            //return
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ModifyWhenNoItem()
        {        
            //                    clientSession.SendPacket(new ModalPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                        Type = 1
            //                    });
            throw new NotImplementedException();
        }


        [TestMethod]
        public void ModifyWhenOtherSeller()
        {
            //                    clientSession.SendPacket(new ModalPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                        Type = 1
            //                    });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ModifyWhenSold()
        {
            //clientSession.SendPacket(new ModalPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.CAN_NOT_MODIFY_SOLD_ITEMS, clientSession.Account.Language),
            //                        Type = 1
            //                    });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ModifyWhenWrongAmount()
        {
            //                    clientSession.SendPacket(new ModalPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                        Type = 1
            //                    });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void ModifyWhenPriceSamePrice()
        {
            //                    clientSession.SendPacket(new ModalPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.STATE_CHANGED_BAZAAR, clientSession.Account.Language),
            //                        Type = 1
            //                    });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Modify()
        {
            //                    clientSession.Character.GenerateSay(
            //                        string.Format(Language.Instance.GetMessageFromKey(LanguageKey.BAZAAR_PRICE_CHANGED, clientSession.Account.Language),
            //                        bz.BazaarItem.Price
            //                    ), SayColorType.Yellow);
            throw new NotImplementedException();
        }
    }
}
