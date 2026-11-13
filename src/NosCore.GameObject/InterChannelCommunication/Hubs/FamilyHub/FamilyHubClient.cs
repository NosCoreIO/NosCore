//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations.Family;
using NosCore.GameObject.Services.FamilyService;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub
{
    public class FamilyHubClient(IMessageBus bus) : IFamilyHub
    {
        public Task<FamilyExtensionPurchase> PurchaseExtensionAsync(long familyId, long characterId, short itemVNum) =>
            bus.RequestAsync<FamilyExtensionPurchase>(new PurchaseFamilyExtensionRequest(familyId, characterId, itemVNum));

        public Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience) =>
            bus.RequestAsync<FamilyExperienceGain?>(new AddFamilyExperienceRequest(familyId, characterId, experience));

        public Task<FamilyExperienceGain?> AdvanceQuestsAsync(long familyId, long? characterId, FamilyQuestObjective objective,
            int amount) => bus.RequestAsync<FamilyExperienceGain?>(new AdvanceFamilyQuestsRequest(familyId, characterId, objective, amount));

        public Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds) =>
            bus.RequestAsync<long?>(new CreateFamilyRequest(name, maxSize, headId, deputyIds));

        public Task<bool> DisbandAsync(long familyId, long headId) =>
            bus.RequestAsync<bool>(new DisbandFamilyRequest(familyId, headId));

        public Task<FamilyMemberAddition> AddMemberAsync(long familyId, long inviterId, long characterId) =>
            bus.RequestAsync<FamilyMemberAddition>(new AddFamilyMemberRequest(familyId, inviterId, characterId));

        public Task<bool> RemoveMemberAsync(long familyId, long actorId, long characterId) =>
            bus.RequestAsync<bool>(new RemoveFamilyMemberRequest(familyId, actorId, characterId));

        public Task<bool> ChangeAsync(long familyId, long actorId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members) =>
            bus.RequestAsync<bool>(new ChangeFamilyRequest(familyId, actorId, family, members));
    }
}
