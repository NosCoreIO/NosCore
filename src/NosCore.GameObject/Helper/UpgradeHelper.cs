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

        public const short SandVNum = 1027;

        public List<UpgradeData> SumHelpers { get; set; }

        private UpgradeHelper()
        {
            LoadSumData();
        }

        private void LoadSumData()
        {
            SumHelpers = new List<UpgradeData>
            {
                new UpgradeData {SuccessRate = 100, Cost = 1500, CellaCost = 5},
                new UpgradeData {SuccessRate = 100, Cost = 3000, CellaCost = 10},
                new UpgradeData {SuccessRate = 85, Cost = 6000, CellaCost = 15},
                new UpgradeData {SuccessRate = 70, Cost = 12000, CellaCost = 20},
                new UpgradeData {SuccessRate = 50, Cost = 24000, CellaCost = 25},
                new UpgradeData {SuccessRate = 20, Cost = 48000, CellaCost = 30}
            };
        }
    }
}
