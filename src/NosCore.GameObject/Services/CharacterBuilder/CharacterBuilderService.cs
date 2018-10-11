using Mapster;
using NosCore.Data.AliveEntities;
using NosCore.GameObject.Services.Inventory;

namespace NosCore.GameObject.Services.CharacterBuilder
{
    public class CharacterBuilderService : ICharacterBuilderService
    {
        private readonly IInventoryService _inventory;

        public CharacterBuilderService(IInventoryService inventory)
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
