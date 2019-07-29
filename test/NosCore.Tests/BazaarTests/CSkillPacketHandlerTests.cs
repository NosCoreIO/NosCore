using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.PacketHandlers.Bazaar;
using System;

namespace NosCore.Tests.BazaarTests
{
    [TestClass]
    public class CSkillPacketHandlerTest
    {
        private CSkillPacketHandler _cskillPacketHandler;

        [TestInitialize]
        public void Setup()
        {
            _cskillPacketHandler = new CSkillPacketHandler();
        }

        [TestMethod]
        public void OpenWhenInShop()
        {
            //                    if (clientSession.Character.InExchangeOrTrade)
            //            {
            //                return;
            //            }
            throw new NotImplementedException();
        }


        [TestMethod]
        public void OpenWhenNoMedal()
        {
            //                clientSession.SendPacket(new InfoPacket
            //                {
            //                    Message = Language.Instance.GetMessageFromKey(LanguageKey.NO_BAZAAR_MEDAL, clientSession.Account.Language)
            //                });
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Open()
        {
            //                byte medal = medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold ? (byte)MedalType.Gold : (byte)MedalType.Silver;
            //    int time = (int)(medalBonus.DateEnd - SystemTime.Now()).TotalHours;
            //    clientSession.SendPacket(new MsgPacket
            //                {
            //                    Message = Language.Instance.GetMessageFromKey(LanguageKey.INFO_BAZAAR, clientSession.Account.Language),
            //                    Type = MessageType.Whisper
            //});
            //                clientSession.SendPacket(new WopenPacket
            //                {
            //                    Type = WindowType.NosBazaar,
            //                    Unknown = medal,
            //                    Unknown2 = (byte) time
            //                });
            throw new NotImplementedException();
        }
    }
}
