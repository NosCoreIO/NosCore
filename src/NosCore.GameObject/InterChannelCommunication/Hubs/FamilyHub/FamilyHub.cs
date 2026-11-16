//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub
{
    public class FamilyHub(IFamilyExtensionService familyExtensionService,
        IFamilyProgressionService familyProgressionService, IFamilyRecordService familyRecordService) : IFamilyHub
    {
        public Task<FamilyExtensionPurchase> PurchaseExtensionAsync(long familyId, long characterId, short itemVNum) =>
            familyExtensionService.PurchaseAsync(familyId, characterId, itemVNum);

        public Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience) =>
            familyProgressionService.AddExperienceAsync(familyId, characterId, experience);

        public Task<FamilyExperienceGain?> AdvanceQuestsAsync(long familyId, long? characterId, FamilyQuestObjective objective,
            int amount, int? parameter) =>
            familyProgressionService.AdvanceQuestsAsync(familyId, characterId, objective, amount, parameter);

        public Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds) =>
            familyRecordService.CreateAsync(name, maxSize, headId, deputyIds);

        public Task<bool> DisbandAsync(long familyId, long headId) => familyRecordService.DisbandAsync(familyId, headId);

        public Task<FamilyMemberAddition> AddMemberAsync(long familyId, long inviterId, long characterId) =>
            familyRecordService.AddMemberAsync(familyId, inviterId, characterId);

        public Task<bool> RemoveMemberAsync(long familyId, long actorId, long characterId) =>
            familyRecordService.RemoveMemberAsync(familyId, actorId, characterId);

        public Task<bool> ChangeAsync(long familyId, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members) =>
            familyRecordService.ChangeAsync(familyId, actorId, family, members);
    }
}
