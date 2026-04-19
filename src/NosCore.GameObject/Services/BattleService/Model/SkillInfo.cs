//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService.Model;

// Unified skill description used by the orchestrator. Resolved once per cast, passed to
// target resolution / damage / buffs so each step stops re-reading the SkillDto.
public sealed record SkillInfo(
    short SkillVnum,
    long CastId,
    short Cooldown,
    short AttackAnimation,
    short CastEffect,
    short Effect,
    byte Type,
    TargetHitType HitType,
    byte Range,
    byte TargetRange,
    byte TargetType,
    byte Element,
    short Duration,
    short MpCost,
    IReadOnlyList<BCardDto> BCards)
{
    public bool IsAoe => HitType is TargetHitType.SingleAoeTargetHit
                         or TargetHitType.AoeTargetHit
                         or TargetHitType.ZoneHit
                         or TargetHitType.SpecialZoneHit;
}
