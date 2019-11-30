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

using System.Collections.Generic;
using System.Dynamic;

namespace NosCore.GameObject.Helper
{
    public sealed class UpgradeHelper
    {
        public static UpgradeHelper Instance => _instance ??= new UpgradeHelper();
        private static UpgradeHelper _instance;

        public byte MaxSumLevel { get; set; }

        public short SandVNum = 1027;

        public List<SumHelper> SumHelpers { get; set; }

        private UpgradeHelper()
        {
            LoadSumData();
        }

        private void LoadSumData()
        {
            MaxSumLevel = 6;
            SumHelpers = new List<SumHelper>
            {
                new SumHelper {SuccessPercent = 100, GoldPrice = 1500, SandAmount = 5},
                new SumHelper {SuccessPercent = 100, GoldPrice = 3000, SandAmount = 10},
                new SumHelper {SuccessPercent = 85, GoldPrice = 6000, SandAmount = 15},
                new SumHelper {SuccessPercent = 70, GoldPrice = 12000, SandAmount = 20},
                new SumHelper {SuccessPercent = 50, GoldPrice = 24000, SandAmount = 25},
                new SumHelper {SuccessPercent = 20, GoldPrice = 48000, SandAmount = 30}
            };
        }
    }
}
