//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Character;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MateService;
using NosCore.Shared.Enumerations;

namespace NosCore.Tests.Shared
{
    public static class TestMates
    {
        private static long _nextTransportId = 3000000;

        public static Mate Pet(long characterId, bool isTeamMember = true, byte level = 1, short vnum = 333) =>
            Create(characterId, MateType.Pet, isTeamMember, level, vnum, "Chicken");

        public static Mate Partner(long characterId, bool isTeamMember = true) =>
            Create(characterId, MateType.Partner, isTeamMember, 1, 317, "Partner");

        private static Mate Create(long characterId, MateType type, bool isTeamMember, byte level, short vnum,
            string name)
        {
            var i18N = new I18NString();
            i18N[RegionType.EN] = name;
            return new Mate
            {
                CharacterId = characterId,
                VNum = vnum,
                MateType = type,
                Level = level,
                Loyalty = 3000,
                Hp = 156,
                Mp = 10,
                IsTeamMember = isTeamMember,
                IsSummonable = true,
                MateTransportId = Interlocked.Increment(ref _nextTransportId),
                NpcMonster = new NpcMonsterDto { NpcMonsterVNum = vnum, Name = i18N, Level = 1, MaxHp = 156, MaxMp = 10 }
            };
        }
    }
}
