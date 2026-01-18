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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.Services.IdService;
using NosCore.Data.Enumerations.Group;
using NosCore.Networking.SessionGroup;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class GroupTests
    {
        private Group? _group;
        private Mock<ISessionGroupFactory>? _sessionGroupFactory;

        [TestInitialize]
        public void Setup()
        {
            _sessionGroupFactory = new Mock<ISessionGroupFactory>();
            _sessionGroupFactory.Setup(x => x.Create()).Returns(new Mock<ISessionGroup>().Object);
            _group = new Group(GroupType.Group, _sessionGroupFactory.Object)
            {
                GroupId = new IdService<Group>(1).GetNextId()
            };
        }

        [TestMethod]
        public void Test_Add_Player()
        {
            _group!.JoinGroup(VisualType.Player, 1);

            Assert.IsFalse(_group.Count == 2);
        }

        [TestMethod]
        public void Test_Remove_Player()
        {
            _group!.JoinGroup(VisualType.Player, 1);

            Assert.IsFalse(_group.Count == 2);

            _group.LeaveGroup(VisualType.Player, 1);

            Assert.IsTrue(_group.Count == 0);
        }

        [TestMethod]
        public void Test_Monster_Join_Group()
        {
            _group!.JoinGroup(VisualType.Monster, 1);

            Assert.IsTrue(_group.IsEmpty);
        }

        [TestMethod]
        public void Test_Leader_Change()
        {
            for (var i = 0; i < (long)_group!.Type; i++)
            {
                _group.JoinGroup(VisualType.Player, i + 1);
            }

            var playerIds = _group.GetPlayerIds().ToList();
            Assert.IsTrue(_group.IsGroupFull && _group.IsGroupLeader(playerIds[0]));

            _group.LeaveGroup(VisualType.Player, playerIds[0]);

            playerIds = _group.GetPlayerIds().ToList();
            Assert.IsTrue(!_group.IsGroupFull && _group.IsGroupLeader(playerIds[0]));
        }
    }
}