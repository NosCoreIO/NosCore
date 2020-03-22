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
using ChickenAPI.Packets.Enumerations;

namespace NosCore.GameObject.Helper
{
    public sealed class CharacterHelper
    {
        private static CharacterHelper _instance;

        private int[][] _criticalDist;
        private int[][] _criticalDistRate;
        private int[][] _criticalHit;
        private int[][] _criticalHitRate;
        private int[][] _distDef;
        private int[][] _distDodge;
        private int[][] _distRate;
        private int[][] _hitDef;
        private int[][] _hitDodge;
        private int[][] _hitRate;
        private int[][] _magicalDef;
        private int[][] _maxDist;
        private int[][] _maxHit;
        private int[][] _minDist;

        // difference between class
        private int[][] _minHit;

        private CharacterHelper()
        {
            LoadSpeedData();
            LoadJobXpData();
            LoadSpxpData();
            LoadHeroXpData();
            LoadXpData();
            LoadHpData();
            LoadMpData();
            LoadStats();
            LoadHpHealth();
            LoadMpHealth();
            LoadHpHealthStand();
            LoadMpHealthStand();
        }

        public static CharacterHelper Instance => _instance ??= new CharacterHelper();

        // STAT DATA

        // same for all class

        public double[] FirstJobXpData { get; private set; }

        public double[] HeroXpData { get; private set; }

        public int[][] HpData { get; private set; }

        public int[] HpHealth { get; private set; }

        public int[] HpHealthStand { get; private set; }

        public int[][] MpData { get; private set; }

        public int[] MpHealth { get; private set; }

        public int[] MpHealthStand { get; private set; }

        public double[] SecondJobXpData { get; private set; }

        public byte[] SpeedData { get; private set; }

        public double[] SpxpData { get; private set; }

        public double[] XpData { get; private set; }

        public double HeroXpLoad(byte heroLevel)
        {
            return heroLevel == 0 ? 1 : HeroXpData[heroLevel - 1];
        }

        public double JobXpLoad(byte jobLevel, CharacterClassType classType)
        {
            return classType == CharacterClassType.Adventurer ? FirstJobXpData[jobLevel - 1]
                : SecondJobXpData[jobLevel - 1];
        }

        public double XpLoad(byte level)
        {
            return XpData[level - 1];
        }

        public static float ExperiencePenalty(byte playerLevel, byte monsterLevel)
        {
            var leveldifference = playerLevel - monsterLevel;
            float penalty;

            // penalty calculation
            switch (leveldifference)
            {
                case 6:
                    penalty = 0.9f;
                    break;

                case 7:
                    penalty = 0.7f;
                    break;

                case 8:
                    penalty = 0.5f;
                    break;

                case 9:
                    penalty = 0.3f;
                    break;

                default:
                    if (leveldifference > 9)
                    {
                        penalty = 0.1f;
                    }
                    else if (leveldifference > 18)
                    {
                        penalty = 0.05f;
                    }
                    else
                    {
                        penalty = 1f;
                    }

                    break;
            }

            return penalty;
        }

        public static float GoldPenalty(byte playerLevel, byte monsterLevel)
        {
            var leveldifference = playerLevel - monsterLevel;
            float penalty;

            // penalty calculation
            switch (leveldifference)
            {
                case 5:
                    penalty = 0.9f;
                    break;

                case 6:
                    penalty = 0.7f;
                    break;

                case 7:
                    penalty = 0.5f;
                    break;

                case 8:
                    penalty = 0.3f;
                    break;

                case 9:
                    penalty = 0.2f;
                    break;

                default:
                    if ((leveldifference > 9) && (leveldifference < 19))
                    {
                        penalty = 0.1f;
                    }
                    else if ((leveldifference > 18) && (leveldifference < 30))
                    {
                        penalty = 0.05f;
                    }
                    else if (leveldifference > 30)
                    {
                        penalty = 0f;
                    }
                    else
                    {
                        penalty = 1f;
                    }

                    break;
            }

            return penalty;
        }

        public static long LoadFairyXpData(long elementRate)
        {
            if (elementRate < 40)
            {
                return elementRate * elementRate + 50;
            }

            return elementRate * elementRate * 3 + 50;
        }

        public static int LoadFamilyXpData(byte familyLevel)
        {
            return familyLevel switch
            {
                1 => 100000,
                2 => 250000,
                3 => 370000,
                4 => 560000,
                5 => 840000,
                6 => 1260000,
                7 => 1900000,
                8 => 2850000,
                9 => 3570000,
                10 => 3830000,
                11 => 4150000,
                12 => 4750000,
                13 => 5500000,
                14 => 6500000,
                15 => 7000000,
                16 => 8500000,
                17 => 9500000,
                18 => 10000000,
                19 => 17000000,
                _ => 999999999,
            };
        }

        public int MagicalDefence(CharacterClassType @class, byte level)
        {
            return _magicalDef[(byte) @class][level];
        }

        public int MaxDistance(CharacterClassType @class, byte level)
        {
            return _maxDist[(byte) @class][level];
        }

        public int MaxHit(CharacterClassType @class, byte level)
        {
            return _maxHit[(byte) @class][level];
        }

        public int MinDistance(CharacterClassType @class, byte level)
        {
            return _minDist[(byte) @class][level];
        }

        public int MinHit(CharacterClassType @class, byte level)
        {
            return _minHit[(int) @class][level];
        }

        public int RarityPoint(short rarity, short lvl)
        {
            var p = rarity switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 4,
                5 => 5,
                6 => 7,
                7 => 10,
                8 => 15,
                _ => rarity * 2,
            };
            return p * (lvl / 5 + 1);
        }

        public int SlPoint(short spPoint, short mode)
        {
            try
            {
                int point;
                switch (mode)
                {
                    case 0:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 28)
                        {
                            point = 10 + (spPoint - 10) / 2;
                        }
                        else if (spPoint <= 88)
                        {
                            point = 19 + (spPoint - 28) / 3;
                        }
                        else if (spPoint <= 168)
                        {
                            point = 39 + (spPoint - 88) / 4;
                        }
                        else if (spPoint <= 268)
                        {
                            point = 59 + (spPoint - 168) / 5;
                        }
                        else if (spPoint <= 334)
                        {
                            point = 79 + (spPoint - 268) / 6;
                        }
                        else if (spPoint <= 383)
                        {
                            point = 90 + (spPoint - 334) / 7;
                        }
                        else if (spPoint <= 391)
                        {
                            point = 97 + (spPoint - 383) / 8;
                        }
                        else if (spPoint <= 400)
                        {
                            point = 98 + (spPoint - 391) / 9;
                        }
                        else if (spPoint <= 410)
                        {
                            point = 99 + (spPoint - 400) / 10;
                        }
                        else
                        {
                            point = 0;
                        }

                        break;

                    case 2:
                        if (spPoint <= 20)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 40)
                        {
                            point = 20 + (spPoint - 20) / 2;
                        }
                        else if (spPoint <= 70)
                        {
                            point = 30 + (spPoint - 40) / 3;
                        }
                        else if (spPoint <= 110)
                        {
                            point = 40 + (spPoint - 70) / 4;
                        }
                        else if (spPoint <= 210)
                        {
                            point = 50 + (spPoint - 110) / 5;
                        }
                        else if (spPoint <= 270)
                        {
                            point = 70 + (spPoint - 210) / 6;
                        }
                        else if (spPoint <= 410)
                        {
                            point = 80 + (spPoint - 270) / 7;
                        }
                        else
                        {
                            point = 0;
                        }

                        break;

                    case 1:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 48)
                        {
                            point = 10 + (spPoint - 10) / 2;
                        }
                        else if (spPoint <= 81)
                        {
                            point = 29 + (spPoint - 48) / 3;
                        }
                        else if (spPoint <= 161)
                        {
                            point = 40 + (spPoint - 81) / 4;
                        }
                        else if (spPoint <= 236)
                        {
                            point = 60 + (spPoint - 161) / 5;
                        }
                        else if (spPoint <= 290)
                        {
                            point = 75 + (spPoint - 236) / 6;
                        }
                        else if (spPoint <= 360)
                        {
                            point = 84 + (spPoint - 290) / 7;
                        }
                        else if (spPoint <= 400)
                        {
                            point = 97 + (spPoint - 360) / 8;
                        }
                        else if (spPoint <= 410)
                        {
                            point = 99 + (spPoint - 400) / 10;
                        }
                        else
                        {
                            point = 0;
                        }

                        break;

                    case 3:
                        if (spPoint <= 10)
                        {
                            point = spPoint;
                        }
                        else if (spPoint <= 50)
                        {
                            point = 10 + (spPoint - 10) / 2;
                        }
                        else if (spPoint <= 110)
                        {
                            point = 30 + (spPoint - 50) / 3;
                        }
                        else if (spPoint <= 150)
                        {
                            point = 50 + (spPoint - 110) / 4;
                        }
                        else if (spPoint <= 200)
                        {
                            point = 60 + (spPoint - 150) / 5;
                        }
                        else if (spPoint <= 260)
                        {
                            point = 70 + (spPoint - 200) / 6;
                        }
                        else if (spPoint <= 330)
                        {
                            point = 80 + (spPoint - 260) / 7;
                        }
                        else if (spPoint <= 410)
                        {
                            point = 90 + (spPoint - 330) / 8;
                        }
                        else
                        {
                            point = 0;
                        }

                        break;
                    default:
                        point = 0;
                        break;
                }

                return point;
            }
            catch
            {
                return 0;
            }
        }

        public int SpPoint(short spLevel, short upgrade)
        {
            var point = spLevel <= 20 ? 0 : (spLevel - 20) * 3;
            return upgrade switch
            {
                1 => point + 5,
                2 => point + 10,
                3 => point + 15,
                4 => point + 20,
                5 => point + 28,
                6 => point + 36,
                7 => point + 46,
                8 => point + 56,
                9 => point + 68,
                10 => point + 80,
                11 => point + 95,
                12 => point + 110,
                13 => point + 128,
                14 => point + 148,
                15 => point + 173,
                _ => upgrade > 15 ? point + 173 + 25 + 5 * (upgrade - 15) : point,
            };
        }

        internal int DarkResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int Defence(CharacterClassType @class, byte level)
        {
            return _hitDef[(byte) @class][level];
        }

        /// <summary>
        ///     Defence rate base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DefenceRate(CharacterClassType @class, byte level)
        {
            return _hitDodge[(byte) @class][level];
        }

        /// <summary>
        ///     Distance Defence base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DistanceDefence(CharacterClassType @class, byte level)
        {
            return _distDef[(byte) @class][level];
        }

        /// <summary>
        ///     Distance Defence Rate base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DistanceDefenceRate(CharacterClassType @class, byte level)
        {
            return _distDodge[(byte) @class][level];
        }

        /// <summary>
        ///     Distance Rate base stats for Character by Class & Level
        /// </summary>
        /// <param name="class"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        internal int DistanceRate(CharacterClassType @class, byte level)
        {
            return _distRate[(byte) @class][level];
        }

        internal int DistCritical(CharacterClassType @class, byte level)
        {
            return _criticalDist[(byte) @class][level];
        }

        internal int DistCriticalRate(CharacterClassType @class, byte level)
        {
            return _criticalDistRate[(byte) @class][level];
        }

        internal int Element(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int ElementRate(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int FireResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int HitCritical(CharacterClassType @class, byte level)
        {
            return _criticalHit[(byte) @class][level];
        }

        internal int HitCriticalRate(CharacterClassType @class, byte level)
        {
            return _criticalHitRate[(byte) @class][level];
        }

        internal int HitRate(CharacterClassType @class, byte level)
        {
            return _hitRate[(byte) @class][level];
        }

        internal int LightResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        internal int WaterResistance(CharacterClassType @class, byte level)
        {
            return 0;
        }

        private void LoadHeroXpData()
        {
            var index = 1;
            var increment = 118980;
            var increment2 = 9120;
            var increment3 = 360;

            HeroXpData = new double[256];
            HeroXpData[0] = 949560;
            for (var lvl = 1; lvl < HeroXpData.Length; lvl++)
            {
                HeroXpData[lvl] = HeroXpData[lvl - 1] + increment;
                increment2 += increment3;
                increment += increment2;
                index++;
                if (index % 10 == 0)
                {
                    increment3 -= index / 10 < 3 ? index / 10 * 30 : 30;
                }
            }
        }

        private void LoadHpData()
        {
            HpData = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };

            // Adventurer HP
            for (var i = 1; i < 256; i++)
            {
                HpData[(int) CharacterClassType.Adventurer][i] = (int) (1 / 2.0 * i * i + 31 / 2.0 * i + 205);
            }

            // Swordsman HP
            for (var i = 0; i < 256; i++)
            {
                uint j = 16;
                var hp = 946;
                var inc = 85;
                while (j <= i)
                {
                    if (j % 5 == 2)
                    {
                        hp += inc / 2;
                        inc += 2;
                    }
                    else
                    {
                        hp += inc;
                        inc += 4;
                    }

                    ++j;
                }

                HpData[(int) CharacterClassType.Swordman][i] = hp;
            }

            // Magician HP
            for (var i = 0; i < 256; i++)
            {
                HpData[(int) CharacterClassType.Magician][i] = (int) ((i + 15) * (i + 15) + i + 15.0 - 465 + 550);
            }

            // Archer HP
            for (var i = 0; i < 256; i++)
            {
                var hp = 680;
                var inc = 35;
                uint j = 16;
                while (j <= i)
                {
                    hp += inc;
                    ++inc;
                    if ((j % 10 == 1) || (j % 10 == 5) || (j % 10 == 8))
                    {
                        hp += inc;
                        ++inc;
                    }

                    ++j;
                }

                HpData[(int) CharacterClassType.Archer][i] = hp;
            }

            // MartialArtist HP
            //TODO: Find real formula, this is currently the swordsman statistics
            for (var i = 0; i < 256; i++)
            {
                uint j = 16;
                var hp = 946;
                var inc = 85;
                while (j <= i)
                {
                    if (j % 5 == 2)
                    {
                        hp += inc / 2;
                        inc += 2;
                    }
                    else
                    {
                        hp += inc;
                        inc += 4;
                    }

                    ++j;
                }

                HpData[(int) CharacterClassType.MartialArtist][i] = hp;
            }
        }

        private void LoadHpHealth()
        {
            HpHealth = new int[5];
            HpHealth[(int) CharacterClassType.Archer] = 60;
            HpHealth[(int) CharacterClassType.Adventurer] = 30;
            HpHealth[(int) CharacterClassType.Swordman] = 90;
            HpHealth[(int) CharacterClassType.Magician] = 30;
            HpHealth[(int) CharacterClassType.MartialArtist] = 90;
        }

        private void LoadHpHealthStand()
        {
            HpHealthStand = new int[5];
            HpHealthStand[(int) CharacterClassType.Archer] = 32;
            HpHealthStand[(int) CharacterClassType.Adventurer] = 25;
            HpHealthStand[(int) CharacterClassType.Swordman] = 26;
            HpHealthStand[(int) CharacterClassType.Magician] = 20;
            HpHealthStand[(int) CharacterClassType.MartialArtist] = 26;
        }

        private void LoadJobXpData()
        {
            // Load JobData
            FirstJobXpData = new double[21];
            SecondJobXpData = new double[256];
            FirstJobXpData[0] = 2200;
            SecondJobXpData[0] = 17600;
            for (var i = 1; i < FirstJobXpData.Length; i++)
            {
                FirstJobXpData[i] = FirstJobXpData[i - 1] + 700;
            }

            for (var i = 1; i < SecondJobXpData.Length; i++)
            {
                var var2 = 400;
                if (i > 3)
                {
                    var2 = 4500;
                }

                if (i > 40)
                {
                    var2 = 15000;
                }

                SecondJobXpData[i] = SecondJobXpData[i - 1] + var2;
            }
        }

        private void LoadMpData()
        {
            MpData = new[]
            {
                new int[257],
                new int[257],
                new int[257],
                new int[257],
                new int[257]
            };
            // ADVENTURER MP
            MpData[(int) CharacterClassType.Adventurer][0] = 60;
            var baseAdventurer = 9;
            for (var i = 1; i < 256; i += 4)
            {
                MpData[(int) CharacterClassType.Adventurer][i] =
                    MpData[(int) CharacterClassType.Adventurer][i - 1] + baseAdventurer;
                MpData[(int) CharacterClassType.Adventurer][i + 1] =
                    MpData[(int) CharacterClassType.Adventurer][i] + baseAdventurer;
                MpData[(int) CharacterClassType.Adventurer][i + 2] =
                    MpData[(int) CharacterClassType.Adventurer][i + 1] + baseAdventurer;
                baseAdventurer++;
                MpData[(int) CharacterClassType.Adventurer][i + 3] =
                    MpData[(int) CharacterClassType.Adventurer][i + 2] + baseAdventurer;
                baseAdventurer++;
            }

            // SWORDSMAN MP
            for (var i = 1; i < 256; i++)
            {
                MpData[(int) CharacterClassType.Swordman][i] = MpData[(int) CharacterClassType.Adventurer][i];
            }

            // ARCHER MP
            for (var i = 0; i < 256; i++)
            {
                MpData[(int) CharacterClassType.Archer][i] = MpData[(int) CharacterClassType.Adventurer][i + 1];
            }

            // MAGICIAN MP
            for (var i = 0; i < 256; i++)
            {
                MpData[(int) CharacterClassType.Magician][i] = 3 * MpData[(int) CharacterClassType.Adventurer][i];
            }

            for (var i = 1; i < 256 - 1; i++)
            {
                MpData[(int) CharacterClassType.MartialArtist][i] = MpData[(int) CharacterClassType.Adventurer][i];
            }
        }

        private void LoadMpHealth()
        {
            MpHealth = new int[5];
            MpHealth[(int) CharacterClassType.Adventurer] = 10;
            MpHealth[(int) CharacterClassType.Swordman] = 30;
            MpHealth[(int) CharacterClassType.Archer] = 50;
            MpHealth[(int) CharacterClassType.Magician] = 80;
            MpHealth[(int) CharacterClassType.MartialArtist] = 30;
        }

        private void LoadMpHealthStand()
        {
            MpHealthStand = new int[5];
            MpHealthStand[(int) CharacterClassType.Adventurer] = 5;
            MpHealthStand[(int) CharacterClassType.Swordman] = 16;
            MpHealthStand[(int) CharacterClassType.Archer] = 28;
            MpHealthStand[(int) CharacterClassType.Magician] = 40;
            MpHealthStand[(int) CharacterClassType.MartialArtist] = 16;
        }

        private void LoadSpeedData()
        {
            SpeedData = new byte[5];
            SpeedData[(int) CharacterClassType.Adventurer] = 11;
            SpeedData[(int) CharacterClassType.Swordman] = 11;
            SpeedData[(int) CharacterClassType.Archer] = 12;
            SpeedData[(int) CharacterClassType.Magician] = 10;
            SpeedData[(int) CharacterClassType.MartialArtist] = 11;
        }

        private void LoadSpxpData()
        {
            // Load SpData
            SpxpData = new double[256];
            SpxpData[0] = 15000;
            SpxpData[19] = 218000;
            for (var i = 1; i < 19; i++)
            {
                SpxpData[i] = SpxpData[i - 1] + 10000;
            }

            for (var i = 20; i < SpxpData.Length; i++)
            {
                SpxpData[i] = SpxpData[i - 1] + 6 * (3 * i * (i + 1) + 1);
            }
        }

        // TODO: Change or Verify
        private void LoadStats()
        {
            _minHit = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _maxHit = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _hitRate = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _criticalHitRate = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _criticalHit = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _minDist = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _maxDist = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _distRate = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _criticalDistRate = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _criticalDist = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _hitDef = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _hitDodge = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _distDef = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _distDodge = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };
            _magicalDef = new[]
            {
                new int[256],
                new int[256],
                new int[256],
                new int[256],
                new int[256]
            };

            for (var i = 0; i < 256; i++)
            {
                // ADVENTURER
                _minHit[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _maxHit[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _hitRate[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _criticalHitRate[(int) CharacterClassType.Adventurer][i] = 0; // sure
                _criticalHit[(int) CharacterClassType.Adventurer][i] = 0; // sure
                _minDist[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _maxDist[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _distRate[(int) CharacterClassType.Adventurer][i] = (i + 9) * 2; // approx
                _criticalDistRate[(int) CharacterClassType.Adventurer][i] = 0; // sure
                _criticalDist[(int) CharacterClassType.Adventurer][i] = 0; // sure
                _hitDef[(int) CharacterClassType.Adventurer][i] = i + 9 / 2; // approx
                _hitDodge[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _distDef[(int) CharacterClassType.Adventurer][i] = (i + 9) / 2; // approx
                _distDodge[(int) CharacterClassType.Adventurer][i] = i + 9; // approx
                _magicalDef[(int) CharacterClassType.Adventurer][i] = (i + 9) / 2; // approx

                // SWORDMAN
                _criticalHitRate[(int) CharacterClassType.Swordman][i] = 0; // approx
                _criticalHit[(int) CharacterClassType.Swordman][i] = 0; // approx
                _criticalDist[(int) CharacterClassType.Swordman][i] = 0; // approx
                _criticalDistRate[(int) CharacterClassType.Swordman][i] = 0; // approx
                _minDist[(int) CharacterClassType.Swordman][i] = i + 12; // approx
                _maxDist[(int) CharacterClassType.Swordman][i] = i + 12; // approx
                _distRate[(int) CharacterClassType.Swordman][i] = 2 * (i + 12); // approx
                _hitDodge[(int) CharacterClassType.Swordman][i] = i + 12; // approx
                _distDodge[(int) CharacterClassType.Swordman][i] = i + 12; // approx
                _magicalDef[(int) CharacterClassType.Swordman][i] = (i + 9) / 2; // approx
                _hitRate[(int) CharacterClassType.Swordman][i] = i + 27; // approx
                _hitDef[(int) CharacterClassType.Swordman][i] = i + 2; // approx

                _minHit[(int) CharacterClassType.Swordman][i] =
                    2 * i + 5; // approx Numbers n such that 10n+9 is prime.
                _maxHit[(int) CharacterClassType.Swordman][i] =
                    2 * i + 5; // approx Numbers n such that 10n+9 is prime.
                _distDef[(int) CharacterClassType.Swordman][i] = i; // approx

                // MAGICIAN
                _hitRate[(int) CharacterClassType.Magician][i] = 0; // sure
                _criticalHitRate[(int) CharacterClassType.Magician][i] = 0; // sure
                _criticalHit[(int) CharacterClassType.Magician][i] = 0; // sure
                _criticalDistRate[(int) CharacterClassType.Magician][i] = 0; // sure
                _criticalDist[(int) CharacterClassType.Magician][i] = 0; // sure

                _minDist[(int) CharacterClassType.Magician][i] = 14 + i; // approx
                _maxDist[(int) CharacterClassType.Magician][i] = 14 + i; // approx
                _distRate[(int) CharacterClassType.Magician][i] = (14 + i) * 2; // approx
                _hitDef[(int) CharacterClassType.Magician][i] = (i + 11) / 2; // approx
                _magicalDef[(int) CharacterClassType.Magician][i] = i + 4; // approx
                _hitDodge[(int) CharacterClassType.Magician][i] = 24 + i; // approx
                _distDodge[(int) CharacterClassType.Magician][i] = 14 + i; // approx

                _minHit[(int) CharacterClassType.Magician][i] =
                    2 * i + 9; // approx Numbers n such that n^2 is of form x^ 2 + 40y ^ 2 with positive x,y.
                _maxHit[(int) CharacterClassType.Magician][i] =
                    2 * i + 9; // approx Numbers n such that n^2 is of form x^2+40y^2 with positive x,y.
                _distDef[(int) CharacterClassType.Magician][i] = 20 + i; // approx

                // ARCHER
                _criticalHitRate[(int) CharacterClassType.Archer][i] = 0; // sure
                _criticalHit[(int) CharacterClassType.Archer][i] = 0; // sure
                _criticalDistRate[(int) CharacterClassType.Archer][i] = 0; // sure
                _criticalDist[(int) CharacterClassType.Archer][i] = 0; // sure

                _minHit[(int) CharacterClassType.Archer][i] = 9 + i * 3; // approx
                _maxHit[(int) CharacterClassType.Archer][i] = 9 + i * 3; // approx
                var add = i % 2 == 0 ? 2 : 4;
                _hitRate[(int) CharacterClassType.Archer][1] = 41;
                _hitRate[(int) CharacterClassType.Archer][i] += add; // approx
                _minDist[(int) CharacterClassType.Archer][i] = 2 * i; // approx
                _maxDist[(int) CharacterClassType.Archer][i] = 2 * i; // approx

                _distRate[(int) CharacterClassType.Archer][i] = 20 + 2 * i; // approx
                _hitDef[(int) CharacterClassType.Archer][i] = i; // approx
                _magicalDef[(int) CharacterClassType.Archer][i] = i + 2; // approx
                _hitDodge[(int) CharacterClassType.Archer][i] = 41 + i; // approx
                _distDodge[(int) CharacterClassType.Archer][i] = i + 2; // approx
                _distDef[(int) CharacterClassType.Archer][i] = i; // approx

                // MartialArtist
                _criticalHitRate[(int) CharacterClassType.MartialArtist][i] = 0; // approx
                _criticalHit[(int) CharacterClassType.MartialArtist][i] = 0; // approx
                _criticalDist[(int) CharacterClassType.MartialArtist][i] = 0; // approx
                _criticalDistRate[(int) CharacterClassType.MartialArtist][i] = 0; // approx
                _minDist[(int) CharacterClassType.MartialArtist][i] = i + 12; // approx
                _maxDist[(int) CharacterClassType.MartialArtist][i] = i + 12; // approx
                _distRate[(int) CharacterClassType.MartialArtist][i] = 2 * (i + 12); // approx
                _hitDodge[(int) CharacterClassType.MartialArtist][i] = i + 12; // approx
                _distDodge[(int) CharacterClassType.MartialArtist][i] = i + 12; // approx
                _magicalDef[(int) CharacterClassType.MartialArtist][i] = (i + 9) / 2; // approx
                _hitRate[(int) CharacterClassType.MartialArtist][i] = i + 27; // approx
                _hitDef[(int) CharacterClassType.MartialArtist][i] = i + 2; // approx
                _minHit[(int) CharacterClassType.MartialArtist][i] =
                    2 * i + 5; // approx Numbers n such that 10n+9 is prime.
                _maxHit[(int) CharacterClassType.MartialArtist][i] =
                    2 * i + 5; // approx Numbers n such that 10n+9 is prime.
                _distDef[(int) CharacterClassType.MartialArtist][i] = i; // approx
            }
        }

        private void LoadXpData()
        {
            // Load XpData
            XpData = new double[256];
            var v = new double[256];
            double var = 1;
            v[0] = 540;
            v[1] = 960;
            XpData[0] = 300;
            for (var i = 2; i < v.Length; i++)
            {
                v[i] = v[i - 1] + 420 + 120 * (i - 1);
            }

            for (var i = 1; i < XpData.Length; i++)
            {
                if (i < 79)
                {
                    if (i == 14)
                    {
                        var = 6 / 3d;
                    }
                    else if (i == 39)
                    {
                        var = 19 / 3d;
                    }
                    else if (i == 59)
                    {
                        var = 70 / 3d;
                    }

                    XpData[i] = Convert.ToInt64(XpData[i - 1] + var * v[i - 1]);
                }
                else
                {
                    if (i == 79)
                    {
                        var = 5000;
                    }
                    else if (i == 82)
                    {
                        var = 9000;
                    }
                    else if (i == 84)
                    {
                        var = 13000;
                    }

                    XpData[i] = Convert.ToInt64(XpData[i - 1] + var * (i + 2) * (i + 2));
                }
            }
        }
    }
}