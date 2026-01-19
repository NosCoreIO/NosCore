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
using NosCore.Data.CommandPackets;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Command;
using NosCore.Tests.Shared;
using SpecLight;

namespace NosCore.PacketHandlers.Tests.Command
{
    [TestClass]
    public class EffectCommandPacketHandlerTests
    {
        private EffectCommandPackettHandler Handler = null!;
        private ClientSession Session = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            Broadcaster.Reset();
            Session = await TestHelpers.Instance.GenerateSessionAsync();
            Handler = new EffectCommandPackettHandler();
        }

        [TestMethod]
        public async Task EffectShouldSendPacket()
        {
            await new Spec("Effect should send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingEffectCommand_, 1)
                .Then(ShouldSendPacketToMap)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task EffectWithDifferentIdShouldSendPacket()
        {
            await new Spec("Effect with different id should send packet")
                .Given(CharacterIsOnMap)
                .WhenAsync(ExecutingEffectCommand_, 100)
                .Then(ShouldSendPacketToMap)
                .ExecuteAsync();
        }

        private void CharacterIsOnMap()
        {
            Session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            Session.Character.MapInstance.LastPackets.Clear();
        }

        private async Task ExecutingEffectCommand_(int effectId)
        {
            await Handler.ExecuteAsync(new EffectCommandPacket { EffectId = effectId }, Session);
        }

        private void ShouldSendPacketToMap()
        {
            Assert.IsTrue(Session.Character.MapInstance.LastPackets.Any());
        }
    }
}
