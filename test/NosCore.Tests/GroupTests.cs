//  __  _  __    __   ___ __  ___ ___  
// |  \| |/__\ /' _/ / _//__\| _ \ __| 
// | | ' | \/ |`._`.| \_| \/ | v / _|  
// |_|\__|\__/ |___/ \__/\__/|_|_\___| 
// 
// Copyright (C) 2018 - NosCore
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

using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;

namespace NosCore.Tests
{
    [TestClass]
    public class GroupTests
    {
        private INamedEntity _entity;
        private Group _group;

        [TestInitialize]
        public void Setup()
        {
            _group = new Group(GroupType.Group);

            GroupAccess.Instance.Groups = new ConcurrentDictionary<long, Group>();

            _group.GroupId = GroupAccess.Instance.GetNextGroupId();
        }

        [TestMethod]
        public void Test_Add_Player()
        {
            _entity = new Character
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            };

            _group.JoinGroup(_entity);

            Assert.IsFalse(_group.Count == 2);
        }

        [TestMethod]
        public void Test_Remove_Player()
        {
            _entity = new Character
            {
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = 1,
                MapId = 1,
                State = CharacterState.Active
            };

            _group.JoinGroup(_entity);

            Assert.IsFalse(_group.Count == 2);

            _group.LeaveGroup(_entity);

            Assert.IsTrue(_group.Count == 0);
        }

        [TestMethod]
        public void Test_Monster_Join_Group()
        {
            _entity = new MapMonster
            {
                Name = "test"
            };

            _group.JoinGroup(_entity);

            Assert.IsTrue(!_group.IsEmpty);
        }
    }
}