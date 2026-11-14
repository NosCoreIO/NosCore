//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Buff;
using NosCore.Data.Enumerations.Group;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.GroupService;
using NosCore.Packets.ServerPackets.Parcel;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.GroupService
{
    [TestClass]
    public class GroupBarTests
    {
        private TestWorld World;
        private ClientSession _member;

        [TestInitialize]
        public async Task SetupAsync()
        {
            World = await TestWorld.CreateAsync();
            _member = await World.CreateSessionAsync();
        }

        [TestMethod]
        public void AMemberBarCarriesTheTimedBuffs()
        {
            Buff(116, isStatic: false);
            Buff(131, isStatic: true);

            CollectionAssert.AreEqual(new[] { 116 }, Bar().BuffIds);
        }

        [TestMethod]
        public void AMemberBarShowsTheMorphOnlyWhileTransformed()
        {
            _member.Character.Morph = 2;

            Assert.AreEqual((short)0, Bar().Morph);

            _member.Character.UseSp = true;

            Assert.AreEqual((short)2, Bar().Morph);
        }

        private PstPacket Bar()
        {
            var group = new Group(GroupType.Group, World.SessionGroupFactory) { GroupId = new IdService<Group>(1).GetNextId() };
            group.JoinGroup(_member.Character);
            return group.GeneratePst().Single();
        }

        private void Buff(short cardId, bool isStatic) =>
            _member.Character.BuffState().NotNull()[cardId] = new BuffInstance(cardId, BuffType.Good, null,
                Instant.MinValue, Instant.MaxValue, Array.Empty<BCardDto>(), Static: isStatic);
    }
}
