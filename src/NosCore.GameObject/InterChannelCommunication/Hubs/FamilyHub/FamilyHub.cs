//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.Extensions.DependencyInjection;
using NosCore.GameObject.Services.FamilyService;
using System;
using System.Threading.Tasks;

namespace NosCore.GameObject.InterChannelCommunication.Hubs.FamilyHub
{
    public class FamilyHub(IFamilyExtensionService familyExtensionService) : IFamilyHub
    {
        public Task<FamilyExtensionPurchase> PurchaseExtensionAsync(long familyId, long characterId, short itemVNum) =>
            familyExtensionService.PurchaseAsync(familyId, characterId, itemVNum);

        public static async Task<Reply<FamilyExtensionPurchase>> Handle(PurchaseFamilyExtensionRequest r, IServiceProvider services) =>
            new(await services.GetRequiredService<FamilyHub>().PurchaseExtensionAsync(r.FamilyId, r.CharacterId, r.ItemVNum));
    }
}
