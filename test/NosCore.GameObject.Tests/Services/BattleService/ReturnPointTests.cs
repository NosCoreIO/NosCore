//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.GameObject.Ecs;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.BattleService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class ReturnPointTests
    {
        private const short Act1Map = 2550;
        private const short UntypedMap = 9999;
        private List<RespawnDto> _respawns;
        private PlayerComponentBundle _character;
        private RespawnService _service;

        [TestInitialize]
        public async Task SetupAsync()
        {
            var world = await TestWorld.CreateAsync();
            _character = (await world.CreateSessionAsync()).Character;
            _respawns = _character.Respawns;
            _respawns.Clear();
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
            Assert.IsNull(_service.GetReturnPoint(_character, Act1Map));
        }

        [TestMethod]
        public void SavedReturnPointIsReturned()
        {
            _service.SetReturnPoint(_character, Act1Map, 20, 33);

            Assert.AreEqual(((short)Act1Map, (short)20, (short)33), _service.GetReturnPoint(_character, Act1Map));
        }

        [TestMethod]
        public void ReturnPointIsKeptApartFromThePlaceOfRevival()
        {
            _service.SetRespawnPoint(_character, Act1Map, 5, 5);
            _service.SetReturnPoint(_character, Act1Map, 20, 33);
            _service.SetReturnPoint(_character, Act1Map, 21, 34);

            Assert.AreEqual(2, _respawns.Count);
            Assert.AreEqual(((short)Act1Map, (short)21, (short)34), _service.GetReturnPoint(_character, Act1Map));
            Assert.AreEqual((short)5, _respawns.Find(r => r.RespawnMapTypeId == (long)RespawnType.DefaultAct1).NotNull().X);
        }

        [TestMethod]
        public void MapWithoutTypeSharesTheAct1ReturnPoint()
        {
            _service.SetReturnPoint(_character, UntypedMap, 20, 33);

            Assert.AreEqual((long)RespawnType.ReturnAct1, _respawns.Single().RespawnMapTypeId);
            Assert.AreEqual(((short)UntypedMap, (short)20, (short)33), _service.GetReturnPoint(_character, Act1Map));
        }
    }
}
