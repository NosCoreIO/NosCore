using System;
using System.Collections.Generic;
using System.Text;

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
