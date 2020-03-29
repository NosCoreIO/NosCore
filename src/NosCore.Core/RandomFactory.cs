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

using System;
using System.Threading;

namespace NosCore.Core
{
    public class RandomFactory : IDisposable
    {
        private static RandomFactory? _instance;
        private static int _seed = Environment.TickCount;

        private readonly ThreadLocal<Random> _random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        private RandomFactory()
        {
        }

        public static RandomFactory Instance => _instance ??= new RandomFactory();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int RandomNumber()
        {
            return RandomNumber(0, 100);
        }

        public int RandomNumber(int min, int max)
        {
            return _random.Value!.Next(min, max);
        }

        protected virtual void Dispose(bool disposing)
        {
            _random.Dispose();
        }
    }
}