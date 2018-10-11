using NosCore.Data.AliveEntities;

namespace NosCore.GameObject.Services.CharacterBuilder
{
    public interface ICharacterBuilderService
    {
        Character LoadCharacter(CharacterDTO characterDto);
    }
}
