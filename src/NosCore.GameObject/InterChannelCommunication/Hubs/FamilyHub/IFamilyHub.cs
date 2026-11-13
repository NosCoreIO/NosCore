//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;

public interface IFamilyHub
{
    Task<FamilyExtensionPurchase> PurchaseExtensionAsync(long familyId, long characterId, short itemVNum);

    Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience);

    Task<FamilyExperienceGain?> AdvanceQuestsAsync(long familyId, FamilyQuestObjective objective, int amount);

    Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds);

    Task<bool> DisbandAsync(long familyId, long headId);

    Task<FamilyMemberAddition> AddMemberAsync(long familyId, long inviterId, long characterId);

    Task<bool> RemoveMemberAsync(long familyId, long actorId, long characterId);

    Task<bool> ChangeAsync(long familyId, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members);
}
