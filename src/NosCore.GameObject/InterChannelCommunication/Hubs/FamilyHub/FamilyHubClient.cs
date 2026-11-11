//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.FamilyService;
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
    }
}
