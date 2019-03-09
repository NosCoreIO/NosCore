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

using Autofac;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.GameObject;
using NosCore.GameObject.DependancyInjection;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Providers.ItemProvider.Item;
using ItemInstance = NosCore.Database.Entities.ItemInstance;

namespace NosCore.Tests
{
    [TestClass]
    public class MapperTests
    {
        private readonly Adapter _adapter = new Adapter();

        [TestInitialize]
        public void Setup()
        {
            var dependancyResolverMock = new Mock<IDependencyResolver>();
            dependancyResolverMock.Setup(s => s.Resolve<Character>()).Returns(new Character(null, null, null));
            new Mapper(dependancyResolverMock.Object);
        }

        [TestMethod]
        public void GoToDtoMappingWorks()
        {
            var monsterGo = new MapMonster();
            var monsterDto = _adapter.Adapt<MapMonsterDto>(monsterGo);
            Assert.IsNotNull(monsterDto);
        }

        [TestMethod]
        public void DtoToGoMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterGo = _adapter.Adapt<MapMonster>(monsterDto);
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void EntityToGoMappingWorks()
        {
            var monsterEntity = new Database.Entities.MapMonster();
            var monsterGo = _adapter.Adapt<MapMonster>(monsterEntity);
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void GoToEntityMappingWorks()
        {
            var monsterGo = new MapMonster();
            var monsterEntity = _adapter.Adapt<Database.Entities.MapMonster>(monsterGo);
            Assert.IsNotNull(monsterEntity);
        }

        [TestMethod]
        public void DtoToEntityMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterEntity = _adapter.Adapt<Database.Entities.MapMonster>(monsterDto);
            Assert.IsNotNull(monsterEntity);
        }

        [TestMethod]
        public void EntityToDtoMappingWorks()
        {
            var monsterEntity = new Database.Entities.MapMonster();
            var monsterGo = _adapter.Adapt<MapMonster>(monsterEntity);
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceEntity()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstanceEntity = _adapter.Adapt<ItemInstance>(wearableInstanceGo);
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceGo()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstanceGo =
                _adapter.Adapt<GameObject.Providers.ItemProvider.Item.ItemInstance>(wearableInstanceGo);
            Assert.IsNotNull(itemInstanceGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceDto()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstancedto = _adapter.Adapt<ItemInstanceDto>(wearableInstanceGo);
            Assert.IsNotNull(itemInstancedto);
        }

        [TestMethod]
        public void ItemInstanceToItemInstanceEntity()
        {
            var itemInstanceGo = new GameObject.Providers.ItemProvider.Item.ItemInstance(new Item());
            var itemInstanceEntity = _adapter.Adapt<ItemInstance>(itemInstanceGo);
            Assert.IsNotNull(itemInstanceEntity);
        }


        [TestMethod]
        public void ItemInstanceDtoToItemInstanceEntity()
        {
            var itemInstanceDto = new ItemInstanceDto();
            var itemInstanceEntity = _adapter.Adapt<ItemInstance>(itemInstanceDto);
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableDtoToSpecialistDtoShouldFail()
        {
            var wearableInstanceDto = new WearableInstanceDto();
            var specialistInstanceDto = _adapter.Adapt<SpecialistInstanceDto>(wearableInstanceDto);
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void WearableToSpecialistDtoShouldFail()
        {
            var wearableInstance = new WearableInstance();
            var specialistInstanceDto = _adapter.Adapt<SpecialistInstanceDto>(wearableInstance);
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void SpecialistDtoToSpecialistShouldPass()
        {
            var specialistInstanceDto = new SpecialistInstanceDto();
            var specialistInstance = _adapter.Adapt<SpecialistInstance>(specialistInstanceDto);
            Assert.IsNotNull(specialistInstance);
        }

        [TestMethod]
        public void BoxDtoToBoxShouldPass()
        {
            var boxInstanceDto = new BoxInstanceDto();
            var boxInstance = _adapter.Adapt<BoxInstance>(boxInstanceDto);
            Assert.IsNotNull(boxInstance);
        }

        [TestMethod]
        public void UsableDtoToUsableShouldPass()
        {
            var usableInstanceDto = new UsableInstanceDto();
            var usableInstance = _adapter.Adapt<UsableInstance>(usableInstanceDto);
            Assert.IsNotNull(usableInstance);
        }
    }
}