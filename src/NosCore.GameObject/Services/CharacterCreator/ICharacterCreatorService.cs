using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.AliveEntities;

namespace NosCore.GameObject.Services
{
    public interface ICharacterCreatorService
    {
        Character LoadCharacter(CharacterDTO characterDto);
    }
}
