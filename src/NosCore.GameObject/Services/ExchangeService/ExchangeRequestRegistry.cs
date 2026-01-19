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

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeRequestRegistry : IExchangeRequestRegistry
    {
        private readonly ConcurrentDictionary<long, ExchangeData> _exchangeDatas = new();
        private readonly ConcurrentDictionary<long, long> _initiatorToTarget = new();
        private readonly ConcurrentDictionary<long, long> _targetToInitiator = new();

        public ExchangeData? GetExchangeData(long characterId) =>
            _exchangeDatas.TryGetValue(characterId, out var data) ? data : null;

        public void SetExchangeData(long characterId, ExchangeData data) =>
            _exchangeDatas[characterId] = data;

        public bool RemoveExchangeData(long characterId) =>
            _exchangeDatas.TryRemove(characterId, out _);

        public long? GetExchangeRequest(long characterId) =>
            _initiatorToTarget.TryGetValue(characterId, out var targetId) ? targetId : null;

        public KeyValuePair<long, long>? GetExchangeRequestPair(long characterId)
        {
            if (_initiatorToTarget.TryGetValue(characterId, out var target))
            {
                return new KeyValuePair<long, long>(characterId, target);
            }

            if (_targetToInitiator.TryGetValue(characterId, out var initiator))
            {
                return new KeyValuePair<long, long>(initiator, characterId);
            }

            return null;
        }

        public void SetExchangeRequest(long characterId, long targetCharacterId)
        {
            _initiatorToTarget[characterId] = targetCharacterId;
            _targetToInitiator[targetCharacterId] = characterId;
        }

        public bool RemoveExchangeRequest(long characterId)
        {
            if (_initiatorToTarget.TryRemove(characterId, out var target))
            {
                _targetToInitiator.TryRemove(target, out _);
                return true;
            }

            if (_targetToInitiator.TryRemove(characterId, out var initiator))
            {
                _initiatorToTarget.TryRemove(initiator, out _);
                return true;
            }

            return false;
        }

        public bool HasExchange(long characterId) =>
            _initiatorToTarget.ContainsKey(characterId) || _targetToInitiator.ContainsKey(characterId);
    }
}
