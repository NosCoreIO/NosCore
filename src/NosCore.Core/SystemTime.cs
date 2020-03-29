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

namespace NosCore.Core
{
    public static class SystemTime
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static Func<DateTime> Now = () => DateTime.UtcNow;
#pragma warning restore CA2211 // Non-constant fields should not be visible

        public static void Freeze()
        {
            Freeze(new DateTime(2000, 1, 1));
        }

        public static void Freeze(DateTime time)
        {
            Now = () => time;
        }
    }
}