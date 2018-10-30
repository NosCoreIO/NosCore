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

using Mapster;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.AliveEntities;
using NosCore.GameObject.Mapping;
using NosCore.GameObject.Services.ItemBuilder.Item;

namespace NosCore.Tests
{
    [TestClass]
    public class MapperTests
    {
        [TestInitialize]
        public void Setup()
        {
            Mapper.InitializeMapperItemInstance();
        }

        [TestMethod]
        public void GoToDtoMappingWorks()
        {
            var monsterGo = new GameObject.MapMonster();
            var monsterDto = monsterGo.Adapt<MapMonsterDto>();
            Assert.IsNotNull(monsterDto);
        }

        [TestMethod]
        public void DtoToGoMappingWorks()
        {
            var monsterDto = new MapMonsterDto();
            var monsterGo = monsterDto.Adapt<GameObject.MapMonster>();
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void EntityToGoMappingWorks()
        {
            var monsterEntity = new Database.Entities.MapMonster();
            var monsterGo = monsterEntity.Adapt<GameObject.MapMonster>();
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void GoToEntityMappingWorks()
        {
            var monsterGo = new GameObject.MapMonster();
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
            var monsterGo = monsterEntity.Adapt<GameObject.MapMonster>();
            Assert.IsNotNull(monsterGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceEntity()
        {
            var wearableInstanceGo = new GameObject.Services.ItemBuilder.Item.WearableInstance(new Item());
            var itemInstanceEntity = wearableInstanceGo.Adapt<Database.Entities.ItemInstance>();
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceGo()
        {
            var wearableInstanceGo = new GameObject.Services.ItemBuilder.Item.WearableInstance(new Item());
            var itemInstanceGo = wearableInstanceGo.Adapt<GameObject.Services.ItemBuilder.Item.ItemInstance>();
            Assert.IsNotNull(itemInstanceGo);
        }

        [TestMethod]
        public void WearableInstanceToItemInstanceDto()
        {
            var wearableInstanceGo = new GameObject.Services.ItemBuilder.Item.WearableInstance(new Item());
            var itemInstancedto = wearableInstanceGo.Adapt<NosCore.Data.ItemInstanceDto>();
            Assert.IsNotNull(itemInstancedto);
        }

        [TestMethod]
        public void ItemInstanceToItemInstanceEntity()
        {
            var itemInstanceGo = new GameObject.Services.ItemBuilder.Item.ItemInstance(new Item());
            var itemInstanceEntity = itemInstanceGo.Adapt<Database.Entities.ItemInstance>();
            Assert.IsNotNull(itemInstanceEntity);
        }


        [TestMethod]
        public void ItemInstanceDtoToItemInstanceEntity()
        {
            var itemInstanceDto = new NosCore.Data.ItemInstanceDto();
            var itemInstanceEntity = itemInstanceDto.Adapt<Database.Entities.ItemInstance>();
            Assert.IsNotNull(itemInstanceEntity);
        }

        [TestMethod]
        public void WearableDtoToSpecialistDtoShouldFail()
        {
            var wearableInstanceDto = new NosCore.Data.WearableInstanceDto();
            var specialistInstanceDto = wearableInstanceDto.Adapt<NosCore.Data.SpecialistInstanceDto>();
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void WearableToSpecialistDtoShouldFail()
        {
            var wearableInstance = new GameObject.Services.ItemBuilder.Item.WearableInstance();
            var specialistInstanceDto = wearableInstance.Adapt<NosCore.Data.SpecialistInstanceDto>();
            Assert.IsNull(specialistInstanceDto);
        }

        [TestMethod]
        public void SpecialistDtoToSpecialistShouldPass()
        {
            var specialistInstanceDto = new NosCore.Data.SpecialistInstanceDto();
            var specialistInstance = specialistInstanceDto.Adapt<GameObject.Services.ItemBuilder.Item.SpecialistInstance>();
            Assert.IsNotNull(specialistInstance);
        }

        [TestMethod]
        public void BoxDtoToBoxShouldPass()
        {
            var boxInstanceDto = new NosCore.Data.BoxInstanceDto();
            var boxInstance = boxInstanceDto.Adapt<GameObject.Services.ItemBuilder.Item.BoxInstance>();
            Assert.IsNotNull(boxInstance);
        }

        [TestMethod]
        public void UsableDtoToUsableShouldPass()
        {
            var usableInstanceDto = new NosCore.Data.UsableInstanceDto();
            var usableInstance = usableInstanceDto.Adapt<GameObject.Services.ItemBuilder.Item.UsableInstance>();
            Assert.IsNotNull(usableInstance);
        }
    }
}