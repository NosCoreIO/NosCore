//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.InventoryService;
using System.Collections.Concurrent;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeData
    {
        public ConcurrentDictionary<InventoryItemInstance, short> ExchangeItems { get; set; } = new();

        public long Gold { get; set; }

        public long BankGold { get; set; }

        public bool ExchangeConfirmed { get; set; }
    }
}
