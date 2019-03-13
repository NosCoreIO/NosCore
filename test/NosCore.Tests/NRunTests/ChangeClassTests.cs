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
using DotNetty.Transport.Channels;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Configuration;
using NosCore.Controllers;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.I18N;
using NosCore.Core.Serializing;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.Enumerations.Interaction;
using NosCore.Data.Enumerations.Items;
using NosCore.Data.StaticEntities;
using NosCore.Database;
using NosCore.Database.DAL;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Providers.ItemProvider;
using NosCore.GameObject.Providers.ItemProvider.Item;
using NosCore.GameObject.Providers.MapInstanceProvider;
using NosCore.GameObject.Providers.MapItemProvider;
using NosCore.Packets.ClientPackets;
using NosCore.GameObject.Map;
using NosCore.GameObject.Providers.InventoryService;
using NosCore.GameObject.Providers.NRunProvider;
using NosCore.GameObject.Providers.NRunProvider.Handlers;
using NosCore.Packets.ServerPackets;
using System.Threading.Tasks;

namespace NosCore.Tests.NRunTests
{
    [TestClass]
    public class ChangeClassTests
    {
        private readonly IGenericDao<AccountDto> _accountDao = new GenericDao<Database.Entities.Account, AccountDto>();
        private readonly IGenericDao<PortalDto> _portalDao = new GenericDao<Database.Entities.Portal, PortalDto>();
        private readonly IGenericDao<MapMonsterDto> _mapMonsterDao = new GenericDao<Database.Entities.MapMonster, MapMonsterDto>();
        private readonly IGenericDao<MapNpcDto> _mapNpcDao = new GenericDao<Database.Entities.MapNpc, MapNpcDto>();
        private readonly IGenericDao<CharacterDto> _characterDao = new GenericDao<Database.Entities.Character, CharacterDto>();
        private readonly IGenericDao<CharacterRelationDto> _characterRelationDao = new GenericDao<Database.Entities.CharacterRelation, CharacterRelationDto>();
        private readonly IGenericDao<ShopDto> _shopDao = new GenericDao<Database.Entities.Shop, ShopDto>();
        private readonly IGenericDao<ShopItemDto> _shopItemDao = new GenericDao<Database.Entities.ShopItem, ShopItemDto>();
        private readonly IGenericDao<IItemInstanceDto> _itemInstanceDao = new ItemInstanceDao();
        private readonly Map _map = new Map
        {
            MapId = 0,
            Name = "Map"
        };

        private NpcPacketController _handler;
        private ItemProvider _item;
        private ClientSession _session;

        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>().AfterMapping(dest => Task.Run(() => dest.Initialize()));
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig().ConstructUsing(src => new MapNpc(null, _shopDao, _shopItemDao, new List<NpcMonsterDto>()));
            PacketFactory.Initialize<NoS0575Packet>();
            var contextBuilder =
                new DbContextOptionsBuilder<NosCoreContext>().UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString());
            DataAccessHelper.Instance.InitializeForTest(contextBuilder.Options);

            var account = new AccountDto { Name = "AccountTest", Password = "test".ToSha512() };
            _accountDao.InsertOrUpdate(ref account);

            var npc = new MapNpcDto();
            _mapNpcDao.InsertOrUpdate(ref npc);

            var instanceAccessService = new MapInstanceProvider(new List<MapDto> { _map },
                new MapItemProvider(new List<IHandler<MapItem, Tuple<MapItem, GetPacket>>>()),
                _mapNpcDao,
                _mapMonsterDao, _portalDao, new Adapter());
            instanceAccessService.Initialize();
            var items = new List<ItemDto>
            {
                new Item {Type = PocketType.Main, VNum = 1012, IsDroppable = true},
                new Item {Type = PocketType.Main, VNum = 1013},
                new Item {Type = PocketType.Equipment, VNum = 1, ItemType = ItemType.Weapon},
                new Item {Type = PocketType.Equipment, VNum = 2, EquipmentSlot = EquipmentType.Fairy, Element = 2},
                new Item
                {
                    Type = PocketType.Equipment, VNum = 912, ItemType = ItemType.Specialist, ReputationMinimum = 2,
                    Element = 1
                },
                new Item {Type = PocketType.Equipment, VNum = 924, ItemType = ItemType.Fashion},
                new Item
                {
                    Type = PocketType.Main, VNum = 1078, ItemType = ItemType.Special,
                    Effect = ItemEffectType.DroppedSpRecharger, EffectValue = 10_000, WaitDelay = 5_000
                }
            };

            _item = new ItemProvider(items, new List<IHandler<Item, Tuple<IItemInstance, UseItemPacket>>>());
            var conf = new WorldConfiguration { MaxItemAmount = 999, BackpackSize = 99 };
            _session = new ClientSession(conf,
                new List<PacketController> { new DefaultPacketController(conf, instanceAccessService, null) },
                instanceAccessService, null);
            _handler = new NpcPacketController(new WorldConfiguration(),
                new NrunProvider(new List<IHandler<Tuple<IAliveEntity, NrunPacket>, Tuple<IAliveEntity, NrunPacket>>>
                    {new ChangeClassHandler()}));
            var _chara = new GameObject.Character(new InventoryService(items, _session.WorldConfiguration), null, null, _characterRelationDao, _characterDao, _itemInstanceDao, _accountDao)
            {
                CharacterId = 1,
                Name = "TestExistingCharacter",
                Slot = 1,
                AccountId = account.AccountId,
                MapId = 0,
                State = CharacterState.Active,
                Account = account
            };

            var channelMock = new Mock<IChannel>();

            _session.RegisterChannel(channelMock.Object);
            _session.InitializeAccount(account);
            _session.SessionId = 1;
            _session.SetCharacter(_chara);
            var mapinstance = instanceAccessService.GetBaseMapById(0);

            _session.Character.MapInstance = instanceAccessService.GetBaseMapById(0);
            _session.Character.MapInstance = mapinstance;
            Broadcaster.Instance.RegisterSession(_session);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCantChangeClassLowLevel(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _handler.RegisterSession(_session);
            _handler.NRun(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            });

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                _session.Account.Language) && packet.Type == MessageType.White);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCantChangeClassLowJobLevel(CharacterClassType characterClass)
        {
            _session.Character.JobLevel = 20;
            _handler.RegisterSession(_session);
            _handler.NRun(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            });

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.TOO_LOW_LEVEL,
                _session.Account.Language) && packet.Type == MessageType.White);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCantChangeBadClass(CharacterClassType characterClass)
        {
            _session.Character.Class = characterClass;
            _handler.RegisterSession(_session);
            _handler.NRun(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)CharacterClassType.Swordman
            });

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.NOT_ADVENTURER,
                _session.Account.Language) && packet.Type == MessageType.White);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.MartialArtist)]
        [DataRow(CharacterClassType.Adventurer)]
        public void UserCantChangeToBadClass(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _handler.RegisterSession(_session);
            _handler.NRun(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            });

            Assert.IsTrue(_session.Character.Class == CharacterClassType.Adventurer && _session.Character.Level == 15 &&
                _session.Character.JobLevel == 20);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCanChangeClass(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _handler.RegisterSession(_session);
            _handler.NRun(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            });

            Assert.IsTrue(_session.Character.Class == characterClass && _session.Character.Level == 15 &&
                _session.Character.JobLevel == 1);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Archer)]
        [DataRow(CharacterClassType.Magician)]
        [DataRow(CharacterClassType.Swordman)]
        public void UserCanNotChangeClassWhenEquipment(CharacterClassType characterClass)
        {
            _session.Character.Level = 15;
            _session.Character.JobLevel = 20;
            _session.Character.Inventory.AddItemToPocket(_item.Create(1, 1));
            var item = _session.Character.Inventory.First();
            item.Value.Type = PocketType.Wear;
            _handler.RegisterSession(_session);
            _handler.NRun(new NrunPacket
            {
                VisualType = VisualType.Npc,
                Runner = NrunRunnerType.ChangeClass,
                VisualId = 0,
                Type = (byte)characterClass
            });

            var packet = (MsgPacket)_session.LastPacket;
            Assert.IsTrue(packet.Message == Language.Instance.GetMessageFromKey(LanguageKey.EQ_NOT_EMPTY,
                _session.Account.Language) && packet.Type == MessageType.White);
        }
    }
}