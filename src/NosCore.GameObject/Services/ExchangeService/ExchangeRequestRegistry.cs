//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//
// Copyright (C) 2019 - NosCore
//
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeRequestRegistry : IExchangeRequestRegistry
    {
        private readonly ConcurrentDictionary<long, ExchangeData> _exchangeDatas = new();
        private readonly ConcurrentDictionary<long, long> _exchangeRequests = new();

        public ExchangeData? GetExchangeData(long characterId) =>
            _exchangeDatas.TryGetValue(characterId, out var data) ? data : null;

        public void SetExchangeData(long characterId, ExchangeData data) =>
            _exchangeDatas[characterId] = data;

        public bool RemoveExchangeData(long characterId) =>
            _exchangeDatas.TryRemove(characterId, out _);

        public long? GetExchangeRequest(long characterId) =>
            _exchangeRequests.TryGetValue(characterId, out var targetId) ? targetId : null;

        public KeyValuePair<long, long>? GetExchangeRequestPair(long characterId)
        {
            var pair = _exchangeRequests.FirstOrDefault(k => k.Key == characterId || k.Value == characterId);
            return pair.Key == 0 && pair.Value == 0 ? null : pair;
        }

        public void SetExchangeRequest(long characterId, long targetCharacterId) =>
            _exchangeRequests[characterId] = targetCharacterId;

        public bool RemoveExchangeRequest(long characterId) =>
            _exchangeRequests.TryRemove(characterId, out _);

        public bool HasExchange(long characterId) =>
            _exchangeRequests.Any(k => k.Key == characterId || k.Value == characterId);
    }
}
