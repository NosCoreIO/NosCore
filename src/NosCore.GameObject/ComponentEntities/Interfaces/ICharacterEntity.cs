using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.Enumerations.Character;

namespace NosCore.GameObject.ComponentEntities.Interfaces
{
    public interface ICharacterEntity : INamedEntity
    {
        AuthorityType Authority { get; }

        GenderType Gender { get; set; }

        HairStyleType HairStyle { get; set; }

        HairColorType HairColor { get; set; }

        byte Equipment { get; set; }

        int ReputIcon { get; }

        int DignityIcon { get; }

        long GroupId { get; }
    }
}