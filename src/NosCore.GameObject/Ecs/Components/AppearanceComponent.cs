using NosCore.Packets.Enumerations;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

public record struct AppearanceComponent(
    GenderType Gender,
    HairStyleType HairStyle,
    HairColorType HairColor,
    CharacterClassType Class,
    byte Face,
    byte Size);
