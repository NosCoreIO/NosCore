//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.TransferService;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeData
    {
        public ConcurrentDictionary<InventoryItemInstance, short> ExchangeItems { get; set; } = new();

        public long Gold { get; set; }

        // In gold, not in the thousands the trade window shows.
        public long BankGold { get; set; }

        public bool ExchangeConfirmed { get; set; }

        // Shared by both sides of one trade: it is the lock their offers and confirmations
        // change under, and records whether one side has already taken the trade to settle.
        public ExchangeSettlement Settlement { get; init; } = new();

        public TransferOffer ToOffer() => new(Gold, BankGold, ExchangeItems.ToDictionary(s => s.Key, s => s.Value));
    }

    public sealed class ExchangeSettlement
    {
        public bool IsClaimed { get; set; }

        // Completes when the trade is closed, however it ends.
        public TaskCompletionSource Closed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
