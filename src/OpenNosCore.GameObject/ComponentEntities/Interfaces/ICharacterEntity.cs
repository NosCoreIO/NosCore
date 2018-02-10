using OpenNosCore.Domain.Character;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNosCore.GameObject.ComponentEntities
{
    public interface ICharacterEntity : INamedEntity, IExperiencedEntity
    {
        byte Authority { get; set; }
        GenderType Gender { get; set; }
        HairStyleType HairStyle { get; set; }
        HairColorType HairColor { get; set; }
        byte Equipment { get; set; }
    }
}
