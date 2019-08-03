using ChickenAPI.Packets.ClientPackets.Bazaar;
using ChickenAPI.Packets.ServerPackets.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Bazaar;
using NosCore.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CSkillPacketHandlerTest
    {
        private ClientSession _session;
        private CSkillPacketHandler _cskillPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            TestHelpers.Reset();
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            _session.Character.StaticBonusList = new List<Data.StaticBonusDto>();
            _cskillPacketHandler = new CSkillPacketHandler();
        }

        [TestMethod]
        public void OpenWhenInShop()
        {
            _session.Character.InExchangeOrTrade = true;
            _cskillPacketHandler.Execute(new CSkillPacket(), _session);
            Assert.IsNull(_session.LastPacket.FirstOrDefault());
        }


        [TestMethod]
        public void OpenWhenNoMedal()
        {
            _cskillPacketHandler.Execute(new CSkillPacket(), _session);
            var lastpacket = (InfoPacket)_session.LastPacket.FirstOrDefault(s => s is InfoPacket);
            Assert.IsTrue(lastpacket.Message == Language.Instance.GetMessageFromKey(LanguageKey.NO_BAZAAR_MEDAL, _session.Account.Language));
        }

        [TestMethod]
        public void Open()
        {
            _session.Character.StaticBonusList.Add(new Data.StaticBonusDto
            {
                StaticBonusType = Data.Enumerations.Buff.StaticBonusType.BazaarMedalGold
            });
            _cskillPacketHandler.Execute(new CSkillPacket(), _session);
            var lastpacket = (MsgPacket)_session.LastPacket.FirstOrDefault(s => s is MsgPacket);
            Assert.IsTrue(lastpacket.Message == Language.Instance.GetMessageFromKey(LanguageKey.INFO_BAZAAR, _session.Account.Language));
        }
    }
}
