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

using NosCore.GameObject.Services.InventoryService;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.GameObject
{
    public class Exchange : IDisposable
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _disposed;

        public Exchange(long player1Id, long player2Id)
        {
            Player1Id = player1Id;
            Player2Id = player2Id;
            Player1Data = new ExchangeData();
            Player2Data = new ExchangeData();
        }

        public long Player1Id { get; }
        public long Player2Id { get; }
        public ExchangeData Player1Data { get; }
        public ExchangeData Player2Data { get; }

        public long GetPartnerId(long playerId)
        {
            return playerId == Player1Id ? Player2Id : Player1Id;
        }

        public bool IsParticipant(long playerId)
        {
            return playerId == Player1Id || playerId == Player2Id;
        }

        public ExchangeData GetPlayerData(long playerId)
        {
            return playerId == Player1Id ? Player1Data : Player2Data;
        }

        public ExchangeData GetPartnerData(long playerId)
        {
            return playerId == Player1Id ? Player2Data : Player1Data;
        }

        public void SetGold(long playerId, long gold, long bankGold)
        {
            var data = GetPlayerData(playerId);
            data.Gold = gold;
            data.BankGold = bankGold;
        }

        public void AddItem(long playerId, InventoryItemInstance item, short amount)
        {
            var data = GetPlayerData(playerId);
            data.ExchangeItems.TryAdd(item, amount);
        }

        public void Confirm(long playerId)
        {
            GetPlayerData(playerId).ExchangeConfirmed = true;
        }

        public bool IsConfirmed(long playerId)
        {
            return GetPlayerData(playerId).ExchangeConfirmed;
        }

        public bool BothConfirmed => Player1Data.ExchangeConfirmed && Player2Data.ExchangeConfirmed;

        public async Task<T> ExecuteLockedAsync<T>(Func<Exchange, Task<T>> operation)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                return await operation(this).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ExecuteLockedAsync(Func<Exchange, Task> operation)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await operation(this).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AcquireLockAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
        }

        public void ReleaseLock()
        {
            _lock.Release();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _lock.Dispose();
            }

            _disposed = true;
        }
    }
}
