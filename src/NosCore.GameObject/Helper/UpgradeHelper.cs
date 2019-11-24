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

namespace NosCore.GameObject.Helper
{
    public sealed class UpgradeHelper
    {
        public static UpgradeHelper Instance => _instance ??= new UpgradeHelper();
        private static UpgradeHelper _instance;

        public byte MaxSumLevel { get; set; }

        public short[] SumSuccessPercent { get; set; }

        public int[] SumGoldPrice { get; set; }

        public short[] SumSandAmount { get; set; }

        private UpgradeHelper()
        {
            LoadSumData();
        }

        private void LoadSumData()
        {
            MaxSumLevel = 6;
            SumSuccessPercent = new short[] { 100, 100, 85, 70, 50, 20 };
            SumGoldPrice = new int[] { 1500, 3000, 6000, 12000, 24000, 48000 };
            SumSandAmount = new short[] { 5, 10, 15, 20, 25, 30 };
        }
    }
}
