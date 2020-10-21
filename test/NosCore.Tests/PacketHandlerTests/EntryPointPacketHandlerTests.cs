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

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Data.CommandPackets;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.CharacterScreen;
using NosCore.Tests.Helpers;
using Serilog;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class EntryPointPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Character? _chara;
        private EntryPointPacketHandler? _entryPointPacketHandler;
        private ClientSession? _session;
        private Mock<AuthHttpClient> _authHttpClient = null!;
        private Mock<ConnectedAccountHttpClient> _connectedAccountHttpClient = null!;
        private Mock<ChannelHttpClient> _channelHttpClient = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _chara = _session.Character;
            await TestHelpers.Instance.CharacterDao.TryInsertOrUpdateAsync(_session.Character);
            await _session.SetCharacterAsync(null).ConfigureAwait(false);
            _authHttpClient = new Mock<AuthHttpClient>();
            _connectedAccountHttpClient = new Mock<ConnectedAccountHttpClient>();
            _channelHttpClient = new Mock<ChannelHttpClient>();
            _entryPointPacketHandler =
                new EntryPointPacketHandler(TestHelpers.Instance.CharacterDao, TestHelpers.Instance.AccountDao,  TestHelpers.Instance.MateDao, Logger, _authHttpClient.Object, _connectedAccountHttpClient.Object, _channelHttpClient.Object);
        }

        //[TestMethod]
        //public async Task ConnectionWithInvalidCharacterAsync()
        //{
        //    await _entryPointPacketHandler.ExecuteAsync(new EntryPointPacket
        //    {
        //        Slot = 1,
        //        AccountName = _session!.Account.Name,
        //    }, _session);
        //    _session!.Account = null!;
        //    Assert.IsNull(_session!.Account);
        //}

        //[TestMethod]
        //public async Task ConnectionWithNotInwaitingAccountAsync()
        //{
        //    await _entryPointPacketHandler.ExecuteAsync(new EntryPointPacket
        //    {
        //        Slot = 0,
        //        AccountName = _session!.Account.Name,
        //    }, _session);
        //    _session!.Account = null!;
        //    Assert.IsNull(_session!.Account);
        //}

        //[TestMethod]
        //public async Task ConnectionWithInwaitingAccountAsync()
        //{
        //    await _entryPointPacketHandler.ExecuteAsync(new EntryPointPacket
        //    {
        //        Slot = 0,
        //        AccountName = _session!.Account.Name,
        //    }, _session);
        //    _session!.Account = null!;
        //    Assert.IsNotNull(_session!.Account);
        //}
    }
}