using System;
using System.Collections.Generic;
using System.Text;
using Mapster;
using NosCore.Data.AliveEntities;

namespace NosCore.GameObject.Services
{
    public class CharacterCreatorService : ICharacterCreatorService
    {
        private Inventory _inventory;

        public CharacterCreatorService(Inventory inventory)
        {
            _inventory = inventory;
        }

        public Character LoadCharacter(CharacterDTO characterDto)
        {
            Character character = characterDto.Adapt<Character>();
            character.Inventory = _inventory;
            return character;
        }
    }
}
