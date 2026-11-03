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
using NosCore.Data.Dto;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Ecs.Extensions;

public static class FamilyExtensions
{
    public static GidxPacket GenerateGidx(this PlayerComponentBundle player,
        IGameLanguageLocalizer localizer, RegionType viewerLanguage)
    {
        var family = player.Family;
        if (family == null)
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
            FamilyId = family.FamilyId,
            FamilyName = player.GenerateFamilyTag(localizer, viewerLanguage),
            FamilyLevel = family.FamilyLevel
        };
    }

    public static GInfoPacket? GenerateGInfo(this PlayerComponentBundle player,
        IFamilyExperienceService familyExperienceService)
    {
        var family = player.Family;
        if (family == null)
        {
            return null;
        }

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
            CharacterFamilyAuthority = (Packets.Enumerations.FamilyAuthority)family.AuthorityOf(player.CharacterId),
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

    public static string? GenerateFamilyTag(this PlayerComponentBundle player,
        IGameLanguageLocalizer localizer, RegionType viewerLanguage)
    {
        var family = player.Family;
        if (family == null)
        {
            return null;
        }

        var rank = family.AuthorityOf(player.CharacterId) switch
        {
            FamilyAuthority.Head => LanguageKey.FAMILY_AUTHORITY_HEAD,
            FamilyAuthority.Assistant => LanguageKey.FAMILY_AUTHORITY_ASSISTANT,
            FamilyAuthority.Manager => LanguageKey.FAMILY_AUTHORITY_MANAGER,
            _ => LanguageKey.FAMILY_AUTHORITY_MEMBER
        };

        return $"{family.Name}({localizer[rank, viewerLanguage]})";
    }

    public static GmbrPacket GenerateGmbr(this Family family, IEnumerable<CharacterDto> characters,
        IReadOnlySet<long> connectedCharacterIds)
    {
        var byId = characters.ToDictionary(s => s.CharacterId);
        return new GmbrPacket
        {
            Members = family.Members
                .Where(s => byId.ContainsKey(s.CharacterId))
                .Select(member =>
                {
                    var character = byId[member.CharacterId];
                    return (GmbrSubPacket?)new GmbrSubPacket
                    {
                        CharacterId = member.CharacterId,
                        FamilyId = family.FamilyId,
                        Name = character.Name,
                        Level = character.Level,
                        Class = character.Class,
                        Authority = (Packets.Enumerations.FamilyAuthority)member.Authority,
                        Rank = (Packets.Enumerations.FamilyMemberRank)member.Rank,
                        IsConnected = connectedCharacterIds.Contains(member.CharacterId),
                        HeroLevel = character.HeroLevel
                    };
                }).ToList()
        };
    }

    public static GmsgPacket GenerateGmsg(this Family family) => new()
    {
        Entries = family.Members
            .Select(s => (GmsgSubPacket?)new GmsgSubPacket { CharacterId = s.CharacterId, Message = s.DailyMessage })
            .ToList()
    };

    public static GexpPacket GenerateGexp(this Family family) => new()
    {
        Entries = family.Members
            .Select(s => (GexpSubPacket?)new GexpSubPacket { CharacterId = s.CharacterId, Experience = s.Experience })
            .ToList()
    };
}
