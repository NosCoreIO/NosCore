using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerStateComponent(
    CharacterDto CharacterDto,
    AccountDto Account,
    ScriptDto? Script,
    bool IsChangingMapInstance,
    bool IsDisconnecting,
    bool InShop,
    bool InExchange,
    bool CanFight,
    Instant LastPortal,
    Instant LastSp,
    byte VehicleSpeed
);
