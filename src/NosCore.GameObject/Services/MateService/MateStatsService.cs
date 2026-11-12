//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.MateExperienceService;
using NosCore.Algorithm.MateHpService;
using NosCore.Algorithm.MateMpService;
using NosCore.Data.Enumerations.Character;
using NosCore.GameObject.Infastructure;

namespace NosCore.GameObject.Services.MateService;

public sealed class MateStatsService(IMateExperienceService mateExperienceService, IMateHpService mateHpService,
    IMateMpService mateMpService) : IMateStatsService, ISingletonService
{
    public void ApplyLevel(Mate mate)
    {
        var monster = mate.NpcMonster;
        mate.XpLoad = mate.MateType == MateType.Pet
            ? mateExperienceService.GetPetExperience(mate.Level)
            : mateExperienceService.GetPartnerExperience(mate.Level);
        mate.MaxHp = (int)mateHpService.GetHp(monster.Race, mate.Level, (short)monster.PetInfoVal1) + monster.HpBonus;
        mate.MaxMp = (int)mateMpService.GetMp(monster.Race, mate.Level, (short)monster.PetInfoVal3) + monster.MpBonus;
    }
}
