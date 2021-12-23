//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NosCore.PacketHandlers.Tests.Parcel
{
    [TestClass]
    public class PstClientPacketHandlerTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        //[TestMethod]
        //public async Task Test_SendMessage()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //    //if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
        //    //{
        //    //    return;
        //    //}

        //    //var dest = _characterDao.FirstOrDefaultAsync(s => s.Name == pstClientPacket.ReceiverName);
        //    //if (dest != null)
        //    //{
        //    //    _mailHttpClient.SendMessage(clientSession.Character, dest.CharacterId, pstClientPacket.Title,
        //    //        pstClientPacket.Text);
        //    //    clientSession.SendPacket(clientSession.Character.GenerateSay(
        //    //        Language.Instance.GetMessageFromKey(
        //    //            LanguageKey.MAILED,
        //    //            clientSession.Account.Language), SayColorType.Yellow));
        //    //}
        //    //else
        //    //{
        //    //    clientSession.SendPacket(
        //    //        clientSession.Character.GenerateSay(
        //    //            Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_FOUND,
        //    //                clientSession.Account.Language),
        //    //            SayColorType.Yellow));
        //    //}
        //}

        //[TestMethod]
        //public async Task Test_SendMessageWhenNoDestination()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //    //if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
        //    //{
        //    //    return;
        //    //}

        //    //var dest = _characterDao.FirstOrDefaultAsync(s => s.Name == pstClientPacket.ReceiverName);
        //    //if (dest != null)
        //    //{
        //    //    _mailHttpClient.SendMessage(clientSession.Character, dest.CharacterId, pstClientPacket.Title,
        //    //        pstClientPacket.Text);
        //    //    clientSession.SendPacket(clientSession.Character.GenerateSay(
        //    //        Language.Instance.GetMessageFromKey(
        //    //            LanguageKey.MAILED,
        //    //            clientSession.Account.Language), SayColorType.Yellow));
        //    //}
        //    //else
        //    //{
        //    //    clientSession.SendPacket(
        //    //        clientSession.Character.GenerateSay(
        //    //            Language.Instance.GetMessageFromKey(LanguageKey.USER_NOT_FOUND,
        //    //                clientSession.Account.Language),
        //    //            SayColorType.Yellow));
        //    //}
        //}

        //[TestMethod]
        //public async Task Test_SendMessageWithNoTitle()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //    //if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
        //    //{
        //    //    return;
        //    //}
        //}

        //[TestMethod]
        //public async Task Test_SendMessageWithNoText()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //    //if (string.IsNullOrEmpty(pstClientPacket.Text) || string.IsNullOrEmpty(pstClientPacket.Title))
        //    //{
        //    //    return;
        //    //}
        //}

        //[TestMethod]
        //public async Task Test_ViewMessage()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //    //if (mail == null)
        //    //{
        //    //    return;
        //    //}

        //    //var patch = new JsonPatchDocument<MailDto>();
        //    //patch.Replace(link => link.IsOpened, true);
        //    //_mailHttpClient.ViewGift(mail.MailDto.MailId, patch);
        //    //clientSession.SendPacket(mail.GeneratePostMessage(pstClientPacket.Type));
        //}

        //[TestMethod]
        //public async Task Test_ViewMessageNotFound()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //}

        //[TestMethod]
        //public async Task Test_DeleteMailNotFound()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //}

        //[TestMethod]
        //public async Task Test_DeleteMail()
        //{
        //    // TODO add test on gift
        //    //Assert.Fail();
        //    //if (mail == null)
        //    //{
        //    //    return;
        //    //}

        //    //_mailHttpClient.DeleteGift(pstClientPacket.Id, clientSession.Character.VisualId, isCopy);
        //    //clientSession.SendPacket(
        //    //    clientSession.Character.GenerateSay(
        //    //        Language.Instance.GetMessageFromKey(LanguageKey.MAIL_DELETED,
        //    //            clientSession.Account.Language),
        //    //        SayColorType.Red));
        //}
    }
}