//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

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

        public Task<long?> CreateAsync(string name, byte maxSize, long headId, IReadOnlyList<long> deputyIds) =>
            bus.RequestAsync<long?>(new CreateFamilyRequest(name, maxSize, headId, deputyIds));

        public Task<bool> DisbandAsync(long familyId) =>
            bus.RequestAsync<bool>(new DisbandFamilyRequest(familyId));

        public Task<bool> AddMemberAsync(long familyId, long characterId) =>
            bus.RequestAsync<bool>(new AddFamilyMemberRequest(familyId, characterId));

        public Task<bool> RemoveMemberAsync(long familyId, long characterId) =>
            bus.RequestAsync<bool>(new RemoveFamilyMemberRequest(familyId, characterId));

        public Task<bool> ChangeAsync(long familyId, FamilyChange? family, IReadOnlyList<FamilyMemberChange> members) =>
            bus.RequestAsync<bool>(new ChangeFamilyRequest(familyId, family, members));
    }
}
