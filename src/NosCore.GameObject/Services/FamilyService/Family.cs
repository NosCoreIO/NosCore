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
    /// <summary>
    /// A family, with the members the server currently knows about.
    /// </summary>
    /// <remarks>
    /// The Family, FamilyCharacter and FamilyLog tables have been in the schema since the first
    /// migration and nothing has ever read them. This is the reading half.
    /// </remarks>
    public class Family : FamilyDto
    {
        /// <summary>Every membership row of this family, head included.</summary>
        public IReadOnlyList<FamilyCharacter> Members { get; set; } = [];

        /// <summary>
        /// The family's head — the one member whose authority is Head.
        /// </summary>
        /// <remarks>
        /// Null when the rows say there is none, which should not happen and does when a database
        /// has been edited by hand. The window is drawn without a name rather than not at all.
        /// </remarks>
        public FamilyCharacter? Head =>
            Members.FirstOrDefault(s => s.Authority == FamilyAuthority.Head);

        /// <summary>
        /// The family window: who is in charge, how big the family is, how far the experience bar
        /// has come, and what the manager and the members are each allowed to do.
        /// </summary>
        /// <remarks>
        /// Every field is confirmed against a real capture, which is worth saying because
        /// seventeen fields in a row is exactly where an off-by-one hides:
        ///
        ///     ginfo -Nemesis- Yzigor 0 7 130000 640000 68 70 3 1 1 1 1 2 1 2 coin^afk^go^...
        ///
        /// family name, head's name, head's gender, level 7, 130000 of 640000 experience, 68 of
        /// 70 members, the reader's own authority (3, a plain member), the four manager
        /// permissions, the manager's authority type, the member history permission, the member
        /// authority type, and the family message with its spaces turned into carets.
        /// </remarks>
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
                // Two enums spell the same four ranks — the database's and the packet
                // library's — and they agree value for value, which a test pins so that a
                // future divergence is caught rather than silently reordering everybody's rank.
                CharacterFamilyAuthority = (NosCore.Packets.Enumerations.FamilyAuthority)readerAuthority,
                FamilyManagerCanInvit = ManagerCanInvite,
                FamilyManagerCanNotice = ManagerCanNotice,
                FamilyManagerCanShout = ManagerCanShout,
                FamilyManagerCanGetHistory = ManagerCanGetHistory,
                FamilyManagerAuthorityType = ManagerAuthorityType,
                FamilyMemberCanGetHistory = MemberCanGetHistory,
                FamilyMemberAuthorityType = MemberAuthorityType,
                // The client splits on spaces, so the message travels with carets in their place
                // — the same convention every other free-text field in this protocol uses.
                FamilyMessage = (FamilyMessage ?? string.Empty).Replace(' ', '^')
            };
        }

        /// <summary>
        /// What goes above a character's head and into the info panel: the family's name with the
        /// member's own rank in brackets after it.
        /// </summary>
        /// <remarks>
        /// The capture spells it out — `[NDM](Gardien)`, `KillaBeez(Gardien)`, `Survival(Membre)`
        /// — so the brackets some families have are part of the NAME, and the parentheses are the
        /// rank. It arrives in the account's own language, which is why this needs a localizer
        /// rather than a constant.
        ///
        /// The English wording of the four ranks could not be checked: the capture is a French
        /// session, and Gardien / Membre / Assistant are what it shows. The enum's own names are
        /// used until somebody can read an English client.
        /// </remarks>
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
