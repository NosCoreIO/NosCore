//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.FamilyService;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub;

public interface IFamilyHub
{
    Task<FamilyExtensionPurchase> PurchaseExtensionAsync(long familyId, long characterId, short itemVNum);

    Task<FamilyExperienceGain?> AddExperienceAsync(long familyId, long characterId, int experience);
}
