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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.MinilandProvider;
using NosCore.PacketHandlers.Friend;
using NosCore.PacketHandlers.Miniland;
using NosCore.Packets.ClientPackets.Miniland;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Configuration;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.Tests.MinilandTests
{
    [TestClass]
    public class MlEditPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private Mock<IMinilandProvider>? _minilandProvider;
        private MlEditPacketHandler? _mlEditPacketHandler;

        private ClientSession? _session;
        private Miniland _miniland = null!;
        [TestInitialize]
        public async Task SetupAsync()
        {
            _miniland = new Miniland();
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, null, null, null, Logger, new List<NpcTalkDto>(), TestHelpers.Instance.DistanceCalculator));
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _minilandProvider = new Mock<IMinilandProvider>();
            _minilandProvider.Setup(s => s.GetMiniland(It.IsAny<long>())).Returns(_miniland);
            _mlEditPacketHandler = new MlEditPacketHandler(_minilandProvider.Object);
        }

        [TestMethod]
        public async Task CanChangeMinilandMessageAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "Test",
                Type = 1
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            Assert.AreEqual("test", _miniland.MinilandMessage);
        }

        [TestMethod]
        public async Task CanChangeMinilandMessageWithSpaceAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                MinilandInfo = "Test Test",
                Type = 1
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            Assert.AreEqual("Test^Test",_miniland.MinilandMessage);
        }

        [TestMethod]
        public async Task CanLockMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Lock,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            Assert.AreEqual(MinilandState.Lock, _miniland.State);
        }


        [TestMethod]
        public async Task CanOpenMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Open,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            Assert.AreEqual(MinilandState.Open, _miniland.State);
        }


        [TestMethod]
        public async Task CanPrivateMinilandAsync()
        {
            var mleditPacket = new MLEditPacket()
            {
                Parameter = MinilandState.Private,
                Type = 2
            };
            await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

            Assert.AreEqual(MinilandState.Private, _miniland.State);
        }

        //TODO
        //[TestMethod]
        //public async Task PrivateKickEveryoneButFriendAsync()
        //{
        //    var mleditPacket = new MLEditPacket()
        //    {
        //        Parameter = MinilandState.Private,
        //        Type = 2
        //    };
        //    await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

        //    Assert.AreEqual(MinilandState.Private, _miniland.State);
        //}

        //TODO
        //[TestMethod]
        //public async Task LockKickEveryoneAsync()
        //{
        //    var mleditPacket = new MLEditPacket()
        //    {
        //        Parameter = MinilandState.Lock,
        //        Type = 2
        //    };
        //    await _mlEditPacketHandler!.ExecuteAsync(mleditPacket, _session!).ConfigureAwait(false);

        //    Assert.AreEqual(MinilandState.Private, _miniland.State);
        //}
    }
}