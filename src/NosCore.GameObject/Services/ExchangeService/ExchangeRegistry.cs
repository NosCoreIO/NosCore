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

namespace NosCore.GameObject.Services.ExchangeService
{
    public class ExchangeRegistry : IExchangeRegistry
    {
        private readonly ConcurrentDictionary<long, Exchange> _exchanges = new();

        public Exchange? GetExchange(long playerId)
        {
            return _exchanges.TryGetValue(playerId, out var exchange) ? exchange : null;
        }

        public bool TryRegister(Exchange exchange)
        {
            if (_exchanges.ContainsKey(exchange.Player1Id) || _exchanges.ContainsKey(exchange.Player2Id))
            {
                return false;
            }

            _exchanges[exchange.Player1Id] = exchange;
            _exchanges[exchange.Player2Id] = exchange;
            return true;
        }

        public bool TryUnregister(Exchange exchange)
        {
            var removed1 = _exchanges.TryRemove(exchange.Player1Id, out _);
            var removed2 = _exchanges.TryRemove(exchange.Player2Id, out _);

            if (removed1 || removed2)
            {
                exchange.Dispose();
                return true;
            }

            return false;
        }

        public bool IsInExchange(long playerId)
        {
            return _exchanges.ContainsKey(playerId);
        }
    }
}
