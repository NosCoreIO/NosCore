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

using GraphQL;
using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data;
using NosCore.Data.AliveEntities;
using NosCore.GameObject;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Services.ItemBuilder.Item;
using ItemInstance = NosCore.Database.Entities.ItemInstance;

namespace NosCore.Tests
{
    [TestClass]
    public class MapperTests
    {
        [TestInitialize]
        public void Setup()
        {
            new Mapper(new FuncDependencyResolver(null)).InitializeMapperItemInstance();
        }

        [TestMethod]
        public void GoToDtoMappingWorks()
        {
            var monsterGo = new MapMonster();
            var monsterDto = monsterGo.Adapt<MapMonsterDto>();
            Assert.IsNotNull(monsterDto);
        }

        [TestMethod]
        public void DtoToGoMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterGo = monsterDto.Adapt<MapMonster>();
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void EntityToGoMappingWorks()
        {
            var monsterEntity = new Database.Entities.MapMonster();
            var monsterGo = monsterEntity.Adapt<MapMonster>();
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void GoToEntityMappingWorks()
        {
            var monsterGo = new MapMonster();
            var monsterEntity = monsterGo.Adapt<Database.Entities.MapMonster>();
            Assert.IsNotNull(monsterEntity);
        }

        [TestMethod]
        public void DtoToEntityMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterEntity = monsterDto.Adapt<Database.Entities.MapMonster>();
            Assert.IsNotNull(monsterEntity);
        }

        [TestMethod]
        public void EntityToDtoMappingWorks()
        {
            var monsterEntity = new Database.Entities.MapMonster();
            var monsterGo = monsterEntity.Adapt<MapMonster>();
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceEntity()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstanceEntity = wearableInstanceGo.Adapt<ItemInstance>();
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceGo()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstanceGo = wearableInstanceGo.Adapt<GameObject.Services.ItemBuilder.Item.ItemInstance>();
            Assert.IsNotNull(itemInstanceGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceDto()
        {
            var wearableInstanceGo = new WearableInstance(new Item());
            var itemInstancedto = wearableInstanceGo.Adapt<ItemInstanceDto>();
            Assert.IsNotNull(itemInstancedto);
        }

        [TestMethod]
        public void ItemInstanceToItemInstanceEntity()
        {
            var itemInstanceGo = new GameObject.Services.ItemBuilder.Item.ItemInstance(new Item());
            var itemInstanceEntity = itemInstanceGo.Adapt<ItemInstance>();
            Assert.IsNotNull(itemInstanceEntity);
        }


        [TestMethod]
        public void ItemInstanceDtoToItemInstanceEntity()
        {
            var itemInstanceDto = new ItemInstanceDto();
            var itemInstanceEntity = itemInstanceDto.Adapt<ItemInstance>();
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableDtoToSpecialistDtoShouldFail()
        {
            var wearableInstanceDto = new WearableInstanceDto();
            var specialistInstanceDto = wearableInstanceDto.Adapt<SpecialistInstanceDto>();
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void WearableToSpecialistDtoShouldFail()
        {
            var wearableInstance = new WearableInstance();
            var specialistInstanceDto = wearableInstance.Adapt<SpecialistInstanceDto>();
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void SpecialistDtoToSpecialistShouldPass()
        {
            var specialistInstanceDto = new SpecialistInstanceDto();
            var specialistInstance = specialistInstanceDto.Adapt<SpecialistInstance>();
            Assert.IsNotNull(specialistInstance);
        }

        [TestMethod]
        public void BoxDtoToBoxShouldPass()
        {
            var boxInstanceDto = new BoxInstanceDto();
            var boxInstance = boxInstanceDto.Adapt<BoxInstance>();
            Assert.IsNotNull(boxInstance);
        }

        [TestMethod]
        public void UsableDtoToUsableShouldPass()
        {
            var usableInstanceDto = new UsableInstanceDto();
            var usableInstance = usableInstanceDto.Adapt<UsableInstance>();
            Assert.IsNotNull(usableInstance);
        }
    }
}