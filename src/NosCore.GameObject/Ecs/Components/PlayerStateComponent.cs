using NodaTime;
using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Shared.I18N;

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
    short SpCooldown,
    byte VehicleSpeed,
    IGameLanguageLocalizer GameLanguageLocalizer
);
