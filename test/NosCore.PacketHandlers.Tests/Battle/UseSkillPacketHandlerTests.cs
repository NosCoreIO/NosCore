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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.PacketHandlers.Battle;
using NosCore.Packets.ClientPackets.Battle;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.Battle;
using NosCore.Shared.Enumerations;
using NosCore.Tests.Shared;
using Serilog;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Battle
{
    [TestClass]
    public class UseSkillPacketHandlerTests
    {
        private UseSkillPacketHandler Handler = null!;
        private ClientSession Session = null!;
        private ClientSession TargetSession = null!;
        private Mock<IBattleService> BattleService = null!;
        private readonly ILogger Logger = new Mock<ILogger>().Object;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            TargetSession = await TestHelpers.Instance.GenerateSessionAsync();
            BattleService = new Mock<IBattleService>();

            Handler = new UseSkillPacketHandler(
                Logger,
                TestHelpers.Instance.LogLanguageLocalizer,
                BattleService.Object,
                TestHelpers.Instance.SessionRegistry);
        }

        [TestMethod]
        public async Task UsingSkillWhenVehicledShouldSendCancelPacket()
        {
            await new Spec("Using skill when vehicled should send cancel packet")
                .Given(CharacterIsOnMap)
                .And(CharacterIsVehicled)
                .WhenAsync(UsingSkill)
                .Then(ShouldReceiveCancelPacket)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillOnUnknownVisualTypeShouldBeIgnored()
        {
            await new Spec("Using skill on unknown visual type should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(UsingSkillOnUnknownVisualType)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillOnNonExistentPlayerShouldBeIgnored()
        {
            await new Spec("Using skill on nonexistent player should be ignored")
                .Given(CharacterIsOnMap)
                .WhenAsync(UsingSkillOnNonExistentPlayer)
                .Then(NoPacketShouldBeSent)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task UsingSkillOnExistingPlayerShouldCallBattleService()
        {
            await new Spec("Using skill on existing player should call battle service")
                .Given(CharacterIsOnMap)
                .And(TargetIsOnSameMap)
                .WhenAsync(UsingSkillOnExistingPlayer)
                .Then(BattleServiceShouldBeCalled)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void TargetIsOnSameMap()
        {
            TargetSession.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
        }

        private void CharacterIsVehicled()
        {
            Session.Character.IsVehicled = true;
        }

        private async Task UsingSkill()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = Session.Character.VisualId
            }, Session);
        }

        private async Task UsingSkillOnUnknownVisualType()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = (VisualType)99,
                TargetId = 1
            }, Session);
        }

        private async Task UsingSkillOnNonExistentPlayer()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = 99999
            }, Session);
        }

        private async Task UsingSkillOnExistingPlayer()
        {
            await Handler.ExecuteAsync(new UseSkillPacket
            {
                CastId = 1,
                TargetVisualType = VisualType.Player,
                TargetId = TargetSession.Character.VisualId
            }, Session);
        }

        private void ShouldReceiveCancelPacket()
        {
            var packet = Session.LastPackets.OfType<CancelPacket>().FirstOrDefault();
            Assert.IsNotNull(packet);
        }

        private void NoPacketShouldBeSent()
        {
            Assert.AreEqual(0, Session.LastPackets.Count);
        }

        private void BattleServiceShouldBeCalled()
        {
            BattleService.Verify(x => x.Hit(It.IsAny<NosCore.GameObject.ComponentEntities.Interfaces.ICharacterEntity>(),
                It.IsAny<NosCore.GameObject.ComponentEntities.Interfaces.IAliveEntity>(),
                It.IsAny<HitArguments>()), Times.Once);
        }
    }
}
