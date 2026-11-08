//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NodaTime;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.GameObject.InterChannelCommunication.Hubs.MailHub;
using NosCore.GameObject.InterChannelCommunication.Messages;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Parcel;
using NosCore.Packets.ClientPackets.Parcel;
using NosCore.Packets.ServerPackets.Chats;
using NosCore.Tests.Shared;
using SpecLight;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Tests.Parcel
{
    [TestClass]
    public class PstClientPacketHandlerTests
    {
        private Mock<IMailHub> MailHub = null!;
        private PstClientPacketHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            MailHub = new Mock<IMailHub>();
            Handler = new PstClientPacketHandler(MailHub.Object, new Mock<IDao<CharacterDto, long>>().Object,
                SystemClock.Instance);
        }

        [TestMethod]
        public async Task DeletingANoteRemovesIt()
        {
            await new Spec("Deleting a note removes it")
                .Given(ANoteExists)
                .WhenAsync(DeletingNote_, (short)1)
                .Then(TheNoteShouldBeDeleted)
                .And(TheDeletionShouldBeConfirmed)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task DeletingANoteWithAnIdBelowZeroIsIgnored()
        {
            await new Spec("Deleting a note with an id below zero is ignored")
                .Given(ANoteExists)
                .WhenAsync(DeletingNote_, (short)-1)
                .Then(NoNoteShouldBeDeleted)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task ANoteThatCouldNotBeDeletedIsNotConfirmed()
        {
            await new Spec("A note that could not be deleted is not confirmed")
                .Given(ANoteExists)
                .And(TheNoteWasAlreadyDeleted)
                .WhenAsync(DeletingNote_, (short)1)
                .Then(NoDeletionShouldBeConfirmed)
                .ExecuteAsync();
        }

        private void ANoteExists()
        {
            var characterId = Session.Character.VisualId;
            var note = new MailData { MailId = 1, MailDto = new MailDto { ReceiverId = characterId } };
            MailHub.Setup(s => s.GetMails(It.IsAny<long>(), characterId, false))
                .ReturnsAsync(new List<MailData> { note });
            MailHub.Setup(s => s.DeleteMailAsync(1, characterId, false)).ReturnsAsync(true);
        }

        private void TheNoteWasAlreadyDeleted()
        {
            MailHub.Setup(s => s.DeleteMailAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>())).ReturnsAsync(false);
        }

        private async Task DeletingNote_(short id)
        {
            await Handler.ExecuteAsync(new PstClientPacket { ActionType = 2, Type = 1, Id = id }, Session);
        }

        private void TheNoteShouldBeDeleted()
        {
            var characterId = Session.Character.VisualId;
            MailHub.Verify(s => s.DeleteMailAsync(1, characterId, false), Times.Once);
        }

        private void NoNoteShouldBeDeleted()
        {
            MailHub.Verify(s => s.DeleteMailAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()), Times.Never);
        }

        private void TheDeletionShouldBeConfirmed()
        {
            Assert.IsTrue(Session.LastPackets.OfType<SayiPacket>().Any());
        }

        private void NoDeletionShouldBeConfirmed()
        {
            Assert.IsFalse(Session.LastPackets.OfType<SayiPacket>().Any());
        }
    }
}
