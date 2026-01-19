//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;

namespace NosCore.GameObject.Services.ExchangeService
{
    public interface IExchangeRequestRegistry
    {
        ExchangeData? GetExchangeData(long characterId);
        void SetExchangeData(long characterId, ExchangeData data);
        bool RemoveExchangeData(long characterId);

        long? GetExchangeRequest(long characterId);
        KeyValuePair<long, long>? GetExchangeRequestPair(long characterId);
        void SetExchangeRequest(long characterId, long targetCharacterId);
        bool RemoveExchangeRequest(long characterId);
        bool HasExchange(long characterId);
    }
}
