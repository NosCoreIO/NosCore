using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChickenAPI.Packets.ClientPackets.Drops;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.Data.WebApi;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.Map;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class BlPacketHandlerTests
    {
        private BlPacketHandler _blPacketHandler;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            Broadcaster.Reset();
            WebApiAccess.RegisterBaseAdress();
            WebApiAccess.Instance.MockValues =
                new Dictionary<WebApiRoute, object>
                {
                    {WebApiRoute.Channel, new List<ChannelInfo> {new ChannelInfo()}},
                    {WebApiRoute.ConnectedAccount, new List<ConnectedAccount>()}
                };

            _session = TestHelpers.Instance.GenerateSession();
            _blPacketHandler = new BlPacketHandler();
        }

        [TestMethod]
        public void Test_Distant_Blacklist()
        {
            var targetSession = TestHelpers.Instance.GenerateSession();
            var blPacket = new BlPacket
            {
                CharacterName = targetSession.Character.Name
            };

            _blPacketHandler.Execute(blPacket, _session);
            Assert.IsTrue(_session.Character.CharacterRelations.Any(s =>
                s.Value.RelatedCharacterId == targetSession.Character.CharacterId
                && s.Value.RelationType == CharacterRelationType.Blocked));
        }

    }
}
