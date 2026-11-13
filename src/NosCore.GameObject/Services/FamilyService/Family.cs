//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Family;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.FamilyService
{
    public class Family : FamilyDto
    {
        private const int MaxAssistants = 2;

        public IReadOnlyList<FamilyCharacterDto> Members { get; set; } = [];

        public IReadOnlyList<FamilyQuestStatus> Quests { get; set; } = [];

        /// <summary>The head's name, kept here because the head is usually offline.</summary>
        public string HeadCharacterName { get; set; } = string.Empty;

        /// <summary>Not in the member list means Member: the packet cannot say "none".</summary>
        public FamilyAuthority AuthorityOf(long characterId) =>
            Members.FirstOrDefault(s => s.CharacterId == characterId)?.Authority
            ?? FamilyAuthority.Member;

        public bool CanInvite(long characterId) => AuthorityOf(characterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => ManagerCanInvite,
            _ => false
        };

        public bool CanNotice(long characterId) => AuthorityOf(characterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => ManagerCanNotice,
            _ => false
        };

        /// <summary>A lower value is a higher rank: nobody dismisses the head, their own rank or above.</summary>
        public bool CanDismiss(long actorId, long targetId)
        {
            var target = Members.FirstOrDefault(s => s.CharacterId == targetId);
            return target != null && AuthorityOf(actorId) is FamilyAuthority.Head or FamilyAuthority.Assistant
                && target.Authority > AuthorityOf(actorId);
        }

        public FamilyAuthorityChange CheckAuthorityChange(long actorId, long targetId, FamilyAuthority authority)
        {
            var membership = Members.FirstOrDefault(s => s.CharacterId == targetId);
            if (membership == null)
            {
                return FamilyAuthorityChange.NotInFamily;
            }

            var actorAuthority = Members.FirstOrDefault(s => s.CharacterId == actorId)?.Authority;
            if (actorAuthority is not (FamilyAuthority.Head or FamilyAuthority.Assistant)
                || targetId == actorId || membership.Authority == FamilyAuthority.Head)
            {
                return FamilyAuthorityChange.NotAuthorized;
            }

            switch (authority)
            {
                case FamilyAuthority.Head when actorAuthority != FamilyAuthority.Head:
                case FamilyAuthority.Assistant when actorAuthority != FamilyAuthority.Head:
                    return FamilyAuthorityChange.NotAuthorized;
                case FamilyAuthority.Head when membership.Authority != FamilyAuthority.Assistant:
                    return FamilyAuthorityChange.OnlyAssistantCanBecomeHead;
                case FamilyAuthority.Assistant when Members.Count(s => s.Authority == FamilyAuthority.Assistant) >= MaxAssistants:
                    return FamilyAuthorityChange.TooManyAssistants;
                case FamilyAuthority.Manager or FamilyAuthority.Member
                    when membership.Authority == FamilyAuthority.Assistant && actorAuthority != FamilyAuthority.Head:
                    return FamilyAuthorityChange.NotAuthorized;
            }

            return FamilyAuthorityChange.Changed;
        }

        public bool CanGetHistory(long characterId) => AuthorityOf(characterId) switch
        {
            FamilyAuthority.Head or FamilyAuthority.Assistant => true,
            FamilyAuthority.Manager => ManagerCanGetHistory,
            _ => MemberCanGetHistory
        };
    }
}
