using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.Shared.Enumerations;
using NosCore.Shared.Enumerations.Character;
using NosCore.Shared.Enumerations.Group;
using Character = NosCore.GameObject.Character;
using MapMonster = NosCore.GameObject.MapMonster;

namespace NosCore.Tests
{
    [TestClass]
    public class GroupTests
    {
        private Group _group;

        private INamedEntity _entity;

        [TestInitialize]
        public void Setup()
        {
            _group = new Group(GroupType.Group);

            ServerManager.Instance.Groups = new ConcurrentDictionary<long, Group>();

            _group.GroupId = ServerManager.Instance.GetNextGroupId();
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

            Assert.IsFalse(_group.IsEmpty);
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

            Assert.IsFalse(_group.IsEmpty);

            _group.LeaveGroup(_entity);

            Assert.IsTrue(_group.IsEmpty);
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
