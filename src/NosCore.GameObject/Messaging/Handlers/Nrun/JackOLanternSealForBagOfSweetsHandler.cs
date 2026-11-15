//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using JetBrains.Annotations;
using NosCore.GameObject.Services.NpcExchangeService;
using NosCore.Packets.Enumerations;

namespace NosCore.GameObject.Messaging.Handlers.Nrun
{
    [UsedImplicitly]
    public sealed class JackOLanternSealForBagOfSweetsHandler(INpcExchangeService npcExchangeService) : NpcExchangeHandler(npcExchangeService)
    {
        public override NrunRunnerType Runner => NrunRunnerType.ExchangeForJackOLanternSealPlusHalloweenBlessing;
    }
}
