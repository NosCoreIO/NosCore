//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.WebApi;
using System.Collections.Generic;

namespace NosCore.GameObject.Services.BazaarService
{
    public interface IBazaarRegistry
    {
        IEnumerable<BazaarLink> GetAll();
        BazaarLink? GetById(long bazaarItemId);
        IEnumerable<BazaarLink> GetBySellerId(long sellerId);
        void Register(long bazaarItemId, BazaarLink bazaarLink);
        bool Unregister(long bazaarItemId);
        void Update(long bazaarItemId, BazaarLink bazaarLink);
        int CountBySellerId(long sellerId);
    }
}
