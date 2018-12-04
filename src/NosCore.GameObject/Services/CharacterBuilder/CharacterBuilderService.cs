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
using NosCore.Data.AliveEntities;
using NosCore.GameObject.Services.ExchangeInfo;
using NosCore.GameObject.Services.Inventory;
using NosCore.GameObject.Services.ItemBuilder;

namespace NosCore.GameObject.Services.CharacterBuilder
{
    public class CharacterBuilderService : ICharacterBuilderService
    {
        private readonly IInventoryService _inventory;
        private readonly IItemBuilderService _itemBuilderService;
        private readonly IExchangeInfoService _exchangeInfo;

        public CharacterBuilderService(IInventoryService inventory, IItemBuilderService itemBuilderService)
        public CharacterBuilderService(IInventoryService inventory, IExchangeInfoService exchangeInfo)
        {
            _inventory = inventory;
            _itemBuilderService = itemBuilderService;
            _exchangeInfo = exchangeInfo;
        }

        public Character LoadCharacter(CharacterDto characterDto)
        {
            Character character = characterDto.Adapt<Character>();
            character.Inventory = _inventory;
            character.ItemBuilderService = _itemBuilderService;
            character.ExchangeInfo = _exchangeInfo;
            return character;
        }
    }
}