using NodaTime;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Ecs.Components;

public record struct PlayerStateComponent(
    CharacterDto CharacterDto,
    AccountDto Account,
    ScriptDto? Script,
    bool IsChangingMapInstance,
    bool IsDisconnecting,
    bool InShop,
    bool CanFight,
    Instant LastPortal,
    Instant LastSp,
    byte VehicleSpeed,
    short VehicleVNum,
    Instant ConnectedAt,
    string? SpeechBubble = null,
    Instant SpeechBubbleExpiresAt = default,
    bool IsWaitingForEvent = false,
    Instant ReviveAskedAt = default,
    ArenaTeamType? TalentArenaTeam = null,
    bool InTalentArenaRing = false
);
