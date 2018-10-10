using NosCore.Shared.Enumerations.Character;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface ICharacterEntity : INamedEntity
    {
        byte Authority { get; set; }

        GenderType Gender { get; set; }

        HairStyleType HairStyle { get; set; }

        HairColorType HairColor { get; set; }

        byte Equipment { get; set; }
    }
}