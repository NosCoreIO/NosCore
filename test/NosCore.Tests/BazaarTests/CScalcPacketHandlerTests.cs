using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.PacketHandlers.Bazaar;
using Serilog;
using NosCore.Configuration;
using Moq;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.HttpClients.BazaarHttpClient;
using NosCore.GameObject.Providers.ItemProvider;
using System;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CScalcPacketHandlerTest
    {
        private CScalcPacketHandler _cScalcPacketHandler;
        private ClientSession _session;
        private Mock<IBazaarHttpClient> _bazaarHttpClient;
        private Mock<IItemProvider> _itemProvider;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        [TestInitialize]
        public void Setup()
        {
            var conf = new WorldConfiguration();
            _cScalcPacketHandler = new CScalcPacketHandler(conf, _bazaarHttpClient.Object, _itemProvider.Object, _logger);
        }

        [TestMethod]
        public void RetrieveWhenInExchangeOrTrade()
        {
            //clientSession.SendPacket(new RCScalcPacket { Type = VisualType.Player, Price = 0, RemainingAmount = 0, Amount = 0, Taxes = 0, Total = 0 });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void RetrieveWhenNoItem()
        {
            //clientSession.SendPacket(new RCScalcPacket { Type = VisualType.Player, Price = 0, RemainingAmount = 0, Amount = 0, Taxes = 0, Total = 0 });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void RetrieveWhenNotYourItem()
        {
            //clientSession.SendPacket(new RCScalcPacket { Type = VisualType.Player, Price = 0, RemainingAmount = 0, Amount = 0, Taxes = 0, Total = 0 });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void RetrieveWhenNotEnoughPlace()
        {
            //                    clientSession.SendPacket(new InfoPacket
            //                    {
            //                        Message = Language.Instance.GetMessageFromKey(LanguageKey.NOT_ENOUGH_PLACE,
            //                            clientSession.Account.Language)
            //                    });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void RetrieveWhenMaxGold()
        {
            //                        clientSession.SendPacket(new MsgPacket
            //                        {
            //                            Message = Language.Instance.GetMessageFromKey(LanguageKey.MAX_GOLD,
            //                                clientSession.Account.Language),
            //                            Type = MessageType.Whisper
            //                        });
            throw new NotImplementedException();
        }


        [TestMethod]
        public void Retrieve()
        {
            //                            clientSession.SendPacket(new RCScalcPacket
            //                            {
            //                                Type = VisualType.Player,
            //                                Price = bz.BazaarItem.Price,
            //                                RemainingAmount = (short) (bz.BazaarItem.Amount - bz.ItemInstance.Amount),
            //                                Amount = bz.BazaarItem.Amount,
            //                                Taxes = taxes,
            //                                Total = price + taxes
            //});
            //                            clientSession.HandlePackets(new[] { new CSListPacket { Index = 0, Filter = BazaarStatusType.Default } });
            throw new NotImplementedException();
        }
    }
}
