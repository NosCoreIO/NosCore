//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Core.I18N;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Shared.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.FamilyService
{
    public class Family : FamilyDto
    {
        public IReadOnlyList<FamilyCharacter> Members { get; set; } = [];

        public FamilyCharacter? Head =>
            Members.FirstOrDefault(s => s.Authority == FamilyAuthority.Head);

        public GInfoPacket GenerateGInfo(FamilyAuthority readerAuthority, uint experienceToNextLevel)
        {
            return new GInfoPacket
            {
                FamilyName = Name,
                CharacterName = Head?.CharacterName ?? string.Empty,
                FamilyHeadGenderType = FamilyHeadGender,
                FamilyLevel = FamilyLevel,
                FamilyXp = FamilyExperience,
                MaxFamilyXp = experienceToNextLevel,
                MembersCount = (ushort)Members.Count,
                MembersCapacity = MaxSize,
                CharacterFamilyAuthority = (NosCore.Packets.Enumerations.FamilyAuthority)readerAuthority,
                FamilyManagerCanInvit = ManagerCanInvite,
                FamilyManagerCanNotice = ManagerCanNotice,
                FamilyManagerCanShout = ManagerCanShout,
                FamilyManagerCanGetHistory = ManagerCanGetHistory,
                FamilyManagerAuthorityType = ManagerAuthorityType,
                FamilyMemberCanGetHistory = MemberCanGetHistory,
                FamilyMemberAuthorityType = MemberAuthorityType,
                FamilyMessage = (FamilyMessage ?? string.Empty).Replace(' ', '^')
            };
        }

        public string GenerateFamilyTag(FamilyAuthority authority, IGameLanguageLocalizer localizer,
            RegionType language)
        {
            var rank = authority switch
            {
                FamilyAuthority.Head => LanguageKey.FAMILY_AUTHORITY_HEAD,
                FamilyAuthority.Assistant => LanguageKey.FAMILY_AUTHORITY_ASSISTANT,
                FamilyAuthority.Manager => LanguageKey.FAMILY_AUTHORITY_MANAGER,
                _ => LanguageKey.FAMILY_AUTHORITY_MEMBER
            };

            return $"{Name}({localizer[rank, language]})".Replace(' ', '^');
        }
    }
}
