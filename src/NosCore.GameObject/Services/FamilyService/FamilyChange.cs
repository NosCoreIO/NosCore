//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using FamilyAuthorityType = NosCore.Packets.Enumerations.FamilyAuthorityType;
using GenderType = NosCore.Packets.Enumerations.GenderType;

namespace NosCore.GameObject.Services.FamilyService
{
    // A change travels to the master as data, so only the fields it names are touched there.
    // A null field is left as stored.
    public sealed record FamilyChange
    {
        public string? FamilyMessage { get; init; }

        public GenderType? FamilyHeadGender { get; init; }

        public bool? ManagerCanInvite { get; init; }

        public bool? ManagerCanNotice { get; init; }

        public bool? ManagerCanShout { get; init; }

        public bool? ManagerCanGetHistory { get; init; }

        public FamilyAuthorityType? ManagerAuthorityType { get; init; }

        public bool? MemberCanGetHistory { get; init; }

        public FamilyAuthorityType? MemberAuthorityType { get; init; }

        public void ApplyTo(FamilyDto family)
        {
            family.FamilyMessage = FamilyMessage ?? family.FamilyMessage;
            family.FamilyHeadGender = FamilyHeadGender ?? family.FamilyHeadGender;
            family.ManagerCanInvite = ManagerCanInvite ?? family.ManagerCanInvite;
            family.ManagerCanNotice = ManagerCanNotice ?? family.ManagerCanNotice;
            family.ManagerCanShout = ManagerCanShout ?? family.ManagerCanShout;
            family.ManagerCanGetHistory = ManagerCanGetHistory ?? family.ManagerCanGetHistory;
            family.ManagerAuthorityType = ManagerAuthorityType ?? family.ManagerAuthorityType;
            family.MemberCanGetHistory = MemberCanGetHistory ?? family.MemberCanGetHistory;
            family.MemberAuthorityType = MemberAuthorityType ?? family.MemberAuthorityType;
        }
    }

    public sealed record FamilyMemberChange(long CharacterId)
    {
        public FamilyAuthority? Authority { get; init; }

        public FamilyMemberRank? Rank { get; init; }

        public string? DailyMessage { get; init; }

        public void ApplyTo(FamilyCharacterDto member)
        {
            member.Authority = Authority ?? member.Authority;
            member.Rank = Rank ?? member.Rank;
            member.DailyMessage = DailyMessage ?? member.DailyMessage;
        }
    }
}
