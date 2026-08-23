//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.FamilyExperienceService;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.Family;
using NosCore.Data.Enumerations.I18N;
using NosCore.Packets.ServerPackets.Families;
using NosCore.Shared.Enumerations;
using System.Linq;

namespace NosCore.GameObject.Ecs.Extensions;

public static class FamilyExtensions
{
    /// <summary>
    /// The family tag drawn over a character's head, in the language of whoever is reading it.
    /// </summary>
    /// <remarks>
    /// A capture writes the family field as a single id in all 670 of its gidx lines, and -1
    /// where there is no family:
    ///     gidx 1 521919 5083 [NDM](Gardien) 3
    ///     gidx 1 741328 -1 - 0
    /// </remarks>
    public static GidxPacket GenerateGidx(this PlayerComponentBundle player,
        IGameLanguageLocalizer localizer, RegionType viewerLanguage)
    {
        var membership = player.FamilyCharacter;
        if (membership == null)
        {
            return new GidxPacket
            {
                VisualType = VisualType.Player,
                VisualId = player.VisualId,
                FamilyId = null,
                FamilyName = null,
                FamilyLevel = 0
            };
        }

        return new GidxPacket
        {
            VisualType = VisualType.Player,
            VisualId = player.VisualId,
            FamilyId = membership.FamilyId,
            FamilyName = FamilyTag(membership, localizer, viewerLanguage),
            FamilyLevel = membership.Family.FamilyLevel
        };
    }

    /// <summary>
    /// The family window. Every field is confirmed against a capture, which is worth stating
    /// because seventeen numbers in a row is where an off-by-one hides and this one hands out
    /// the wrong permissions without throwing:
    ///     ginfo -Nemesis- Yzigor 0 7 130000 640000 68 70 3 1 1 1 1 2 1 2 coin^afk^go^rush
    /// </summary>
    public static GInfoPacket? GenerateGInfo(this PlayerComponentBundle player,
        IFamilyExperienceService familyExperienceService)
    {
        var membership = player.FamilyCharacter;
        if (membership == null)
        {
            return null;
        }

        var family = membership.Family;
        return new GInfoPacket
        {
            FamilyName = family.Name,
            CharacterName = family.HeadCharacterName,
            FamilyHeadGenderType = family.FamilyHeadGender,
            FamilyLevel = family.FamilyLevel,
            FamilyXp = family.FamilyExperience,
            MaxFamilyXp = familyExperienceService.GetFamilyExperience(family.FamilyLevel),
            MembersCount = (ushort)family.Members.Count,
            MembersCapacity = family.MaxSize,
            CharacterFamilyAuthority = (Packets.Enumerations.FamilyAuthority)membership.Authority,
            FamilyManagerCanInvit = family.ManagerCanInvite,
            FamilyManagerCanNotice = family.ManagerCanNotice,
            FamilyManagerCanShout = family.ManagerCanShout,
            FamilyManagerCanGetHistory = family.ManagerCanGetHistory,
            FamilyManagerAuthorityType = family.ManagerAuthorityType,
            FamilyMemberCanGetHistory = family.MemberCanGetHistory,
            FamilyMemberAuthorityType = family.MemberAuthorityType,
            FamilyMessage = family.FamilyMessage
        };
    }

    /// <summary>
    /// "Name(Rank)". The capture shows [NDM](Gardien) and Survival(Membre), so the brackets
    /// some families have are part of the name and the parentheses hold the rank.
    /// </summary>
    private static string FamilyTag(Services.FamilyService.FamilyCharacter membership,
        IGameLanguageLocalizer localizer, RegionType viewerLanguage)
    {
        var rank = membership.Authority switch
        {
            FamilyAuthority.Head => LanguageKey.FAMILY_AUTHORITY_HEAD,
            FamilyAuthority.Assistant => LanguageKey.FAMILY_AUTHORITY_ASSISTANT,
            FamilyAuthority.Manager => LanguageKey.FAMILY_AUTHORITY_MANAGER,
            _ => LanguageKey.FAMILY_AUTHORITY_MEMBER
        };

        return $"{membership.Family.Name}({localizer[rank, viewerLanguage]})";
    }
}
