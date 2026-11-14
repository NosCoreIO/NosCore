//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.Data.Enumerations.Items
{
    // What the effect number means on an item of ItemType Teacher, where it does not match ItemEffectType.
    public enum TeacherItemEffectType : ushort
    {
        PetLevelUp = 11,
        PartnerLevelUp = 12,
        PetNameTag = 13,
        PetPickUpTraining = 14,
        PetLevelDown = 16,
        MateSummoningBook = 17,
        PartnerLevelDown = 18,
        ReleasePet = 1000,
        ReleasePartner = 1001
    }
}
