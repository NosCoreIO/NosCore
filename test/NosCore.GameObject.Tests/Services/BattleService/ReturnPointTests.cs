//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class ReturnPointTests
    {
        private const short Act1Map = 2550;
        private const short UntypedMap = 9999;
        private List<RespawnDto> _respawns = null!;
        private Mock<ICharacterEntity> _character = null!;
        private RespawnService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _respawns = new List<RespawnDto>();
            _character = new Mock<ICharacterEntity>();
            _character.SetupGet(c => c.Respawns).Returns(_respawns);
            _character.SetupGet(c => c.CharacterId).Returns(1);
            _service = new RespawnService(
                new List<MapTypeMapDto> { new() { MapId = Act1Map, MapTypeId = (short)MapTypeType.Act1 } },
                new List<MapTypeDto>
                {
                    new()
                    {
                        MapTypeId = (short)MapTypeType.Act1,
                        RespawnMapTypeId = (long)RespawnType.DefaultAct1,
                        ReturnMapTypeId = (long)RespawnType.ReturnAct1
                    }
                },
                new List<RespawnMapTypeDto>());
        }

        [TestMethod]
        public void NoReturnPointUntilOneIsSaved()
        {
            Assert.IsNull(_service.GetReturnPoint(_character.Object, Act1Map));
        }

        [TestMethod]
        public void SavedReturnPointIsReturned()
        {
            _service.SetReturnPoint(_character.Object, Act1Map, 20, 33);

            Assert.AreEqual(((short)Act1Map, (short)20, (short)33), _service.GetReturnPoint(_character.Object, Act1Map));
        }

        [TestMethod]
        public void ReturnPointIsKeptApartFromThePlaceOfRevival()
        {
            _service.SetRespawnPoint(_character.Object, Act1Map, 5, 5);
            _service.SetReturnPoint(_character.Object, Act1Map, 20, 33);
            _service.SetReturnPoint(_character.Object, Act1Map, 21, 34);

            Assert.AreEqual(2, _respawns.Count);
            Assert.AreEqual(((short)Act1Map, (short)21, (short)34), _service.GetReturnPoint(_character.Object, Act1Map));
            Assert.AreEqual((short)5, _respawns.Find(r => r.RespawnMapTypeId == (long)RespawnType.DefaultAct1)!.X);
        }

        [TestMethod]
        public void MapWithoutTypeSharesTheAct1ReturnPoint()
        {
            _service.SetReturnPoint(_character.Object, UntypedMap, 20, 33);

            Assert.AreEqual((long)RespawnType.ReturnAct1, _respawns.Single().RespawnMapTypeId);
            Assert.AreEqual(((short)UntypedMap, (short)20, (short)33), _service.GetReturnPoint(_character.Object, Act1Map));
        }
    }
}
