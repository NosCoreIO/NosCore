namespace NosCore.Domain.Battle
{
    public enum TargetHitType : byte
    {
        SingleTargetHit = 0,
        SingleTargetHitCombo = 1,
        SingleAOETargetHit = 2,
        AOETargetHit = 3,
        ZoneHit = 4,
        SpecialZoneHit = 5
    }
}