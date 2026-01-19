//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Battle
{
    public enum TargetHitType : byte
    {
        SingleTargetHit = 0,
        SingleTargetHitCombo = 1,
        SingleAoeTargetHit = 2,
        AoeTargetHit = 3,
        ZoneHit = 4,
        SpecialZoneHit = 5
    }
}
