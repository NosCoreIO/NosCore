using System;
using System.Collections.Generic;
using System.Text;
using NosCore.Data.AliveEntities;

namespace NosCore.GameObject.Services
{
    public interface ICharacterBuilderService
    {
        Character LoadCharacter(CharacterDTO characterDto);
    }
}
