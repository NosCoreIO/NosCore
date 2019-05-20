using System.Collections.Generic;
using System.Linq;
using ChickenAPI.Packets.ClientPackets.Relations;
using ChickenAPI.Packets.Enumerations;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core;
using NosCore.Core.I18N;
using NosCore.Core.Networking;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.PacketHandlers.Friend;
using NosCore.Tests.Helpers;
using Serilog;
using Character = NosCore.GameObject.Character;

namespace NosCore.Tests.PacketHandlerTests
{
    [TestClass]
    public class FinsPacketHandlerTests
    {
        private FinsPacketHandler _finsPacketHandler;
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig().ConstructUsing(src => new MapNpc(null, null, null, null, _logger));
            Broadcaster.Reset();
            _session = TestHelpers.Instance.GenerateSession();
            var webApiAccess = new Mock<IWebApiAccess>().Object;
            _finsPacketHandler = new FinsPacketHandler(webApiAccess);
        }

        //TODO fix
        //[TestMethod]
        //public void Test_Add_Friend()
        //{
        //    var targetSession = TestHelpers.Instance.GenerateSession();
        //    targetSession.Character.FriendRequestCharacters.TryAdd(0, _session.Character.CharacterId);
        //    var finsPacket = new FinsPacket
        //    {
        //        CharacterId = targetSession.Character.CharacterId,
        //        Type = FinsPacketType.Accepted
        //    };
        //    _finsPacketHandler.Execute(finsPacket, _session);
        //    Assert.IsTrue(_session.Character.CharacterRelations.Any(s =>
        //            s.Value.RelatedCharacterId == targetSession.Character.CharacterId)
        //        && targetSession.Character.CharacterRelations.Any(s =>
        //            s.Value.RelatedCharacterId == _session.Character.CharacterId));
        //}
        //TODO fix
        //[TestMethod]
        //public void Test_Add_Friend_When_Disconnected()
        //{
        //    var finsPacket = new FinsPacket
        //    {
        //        CharacterId = 2,
        //        Type = FinsPacketType.Accepted
        //    };
        //    _finsPacketHandler.Execute(finsPacket, _session);

        //    Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty);
        //}
        //TODO fix
        //[TestMethod]
        //public void Test_Add_Not_Requested_Friend()
        //{
        //    var targetSession = TestHelpers.Instance.GenerateSession();
        //    var finsPacket = new FinsPacket
        //    {
        //        CharacterId = targetSession.Character.CharacterId,
        //        Type = FinsPacketType.Accepted
        //    };
        //    _finsPacketHandler.Execute(finsPacket, _session);
        //    Assert.IsTrue(_session.Character.CharacterRelations.IsEmpty &&
        //        targetSession.Character.CharacterRelations.IsEmpty);
        //}

    }
}
